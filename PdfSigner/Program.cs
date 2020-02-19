using iText.Forms;
using iText.Forms.Fields;
using iText.IO.Image;
using iText.Kernel;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Data;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using iText.Layout;
using iText.Signatures;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Tls;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.X509;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

 class Program {
	 static void Main(string[] args) {
		 if (args.Length < 3) {
			Console.Write("ERROR: Parametros incorrectos, se esperaban 3: 'DNI(con letra)' 'origen.pdf' 'destino.pdf'  ---parametros opcionales: firma_visible(bool), anchura_extra(int), altura_extra(int)) ---default: true, 100, 0");
			return;
		 }
		 X509Certificate2 certificate = loadCertificate(args[0]);

		 bool visibleSignature = true;
		 int increasedWidth = 100;
		 int increasedHeight = 0;         
		 if (args.Length > 3) {
			 visibleSignature = (args[3].ToLower().Equals("true"));
		 }
		 if (args.Length > 4) {
			 increasedWidth = Int32.Parse(args[4]);
		 }
		 if (args.Length > 5) {
			 increasedHeight = Int32.Parse(args[5]);
		 }
		 Console.Write(signPdf(certificate, args[1], args[2], visibleSignature, increasedWidth,increasedHeight));
	}


	 private static X509Certificate2 loadCertificate(String dni) {
		 X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
		 store.Open(OpenFlags.ReadOnly);
		 X509Certificate2Collection certificates = store.Certificates;
		 foreach (X509Certificate2 c in certificates) {
			 if (getCertDni(c).Equals(dni.Trim(' ').ToUpper())) {
				 return c;
			 }
		 }
		 return null;
	 }

	 private static String getCertDni(X509Certificate2 certificate) {
		 return (certificate.Subject).Split('-', ',')[1].Trim(' ');
	 }

	 private static String getCertNameAndDni(X509Certificate2 certificate) {
		 return (certificate.Subject).Split('=', ',')[1].Trim(' ');
	 }

	 public static string signPdf(X509Certificate2 certificate, string sourcePdf, string targetPdf, bool visibleSignature, int increasedWidth, int increasedHeight) {
		 FileStream fileStream = null;
		 try {
			if (certificate == null) {                    
				return "AVISO: No se ha encontrado certificado válido";
			} 
			// open pdf and prepare for signing
			PdfReader objReader = new PdfReader(sourcePdf);
		 
			//to get the number of pages
			PdfDocument doc = new PdfDocument(objReader);
		  
			StampingProperties properties = new StampingProperties();
			//comento esta linea porque hace que la firma no sea visible en google chrome, en funcion de como se haya generado el pdf que estamos utilizando
			//properties.UseAppendMode(); //dont invalidate old signatures
			
			 fileStream =  new FileStream(targetPdf, FileMode.Create);
			 PdfSigner signer = new PdfSigner(objReader, fileStream, properties);

			//la siguiente linea, certifica que el documento no ha sido modificado, el problema es que cualquier firma posterior en el documento invalida las firmas con este nivel
			//signer.SetCertificationLevel(iText.Signatures.PdfSigner.CERTIFIED_FORM_FILLING_AND_ANNOTATIONS);

		   
			if (visibleSignature) {
				PdfSignatureAppearance signatureAppearance = signer.GetSignatureAppearance();
				signatureAppearance.SetReason("");
				signatureAppearance.SetLocation("");
				signatureAppearance.SetReuseAppearance(false); //Indicates that the existing appearances needs to be reused as layer 0.

				int pageNumber = doc.GetNumberOfPages();
				signatureAppearance.SetPageNumber(pageNumber);

				//vamos a recuperar la posicion de la penultima imagen de la pagina a firmar
				iText.Kernel.Geom.Rectangle rect = getSignPositionRect(doc, pageNumber, increasedWidth, increasedHeight);
				signatureAppearance.SetPageRect(rect);

				//load the sign image
				string fileName = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase);
				fileName = fileName + @"\imagenes\" + getCertDni(certificate) + ".jpg";
				fileName = fileName.Replace(@"file:\", "");

				//si el archivo no existe, pude ser porque esta en una unidad de red '//servname' , en vez de 'C:/' )
				if (!(File.Exists(fileName))) {
					fileName = @"\\" + fileName;
				}


				if ((File.Exists(fileName))) {
					//set the signature mode to graphic and add the image to the sign
					Image img = Image.FromFile(fileName);
					iText.IO.Image.ImageData img2 = ImageDataFactory.Create(img, Color.Transparent);
					signatureAppearance.SetRenderingMode(PdfSignatureAppearance.RenderingMode.GRAPHIC_AND_DESCRIPTION);
					signatureAppearance.SetSignatureGraphic(img2);
				} else {
					//set the signature mode to only text
					signatureAppearance.SetRenderingMode(PdfSignatureAppearance.RenderingMode.DESCRIPTION);
				}
			  
				//establecemos el texto de la firma
				signatureAppearance.SetLayer2Text("Firmado digitalmente por: " + getCertNameAndDni(certificate) + "\n" + "Fecha: " + DateTime.Now.Date.Day + "/" + DateTime.Now.Date.Month + "/" + DateTime.Now.Date.Year);

				signer.SetFieldName(signer.GetNewSigFieldName());
			   
				// white background to remove the original pdf sign                    
				var b = new Bitmap(1, 1);
				b.SetPixel(0, 0, Color.White);
				var result = new Bitmap(b, (int)rect.GetWidth(), (int)rect.GetHeight());
				iText.IO.Image.ImageData imgBackground = ImageDataFactory.Create(result, Color.Transparent);
				signatureAppearance.SetImage(imgBackground);   //this is the layer 2 background                    
			}
			
			//load some variables needed to sign the pdf
			IExternalSignature externalSignature = new X509Certificate2Signature(certificate, "SHA-1");
			Org.BouncyCastle.X509.X509Certificate bcCert = new X509CertificateParser().ReadCertificate(certificate.GetRawCertData());
			Org.BouncyCastle.X509.X509Certificate[] chain = new Org.BouncyCastle.X509.X509Certificate[1] { bcCert };
			
			// sign the pdf
			signer.SignDetached(externalSignature, chain, null, null, null, 0, PdfSigner.CryptoStandard.CMS);
		   
			return "OK: Documento firmado como (" + getCertDni(certificate) + ")";

		} catch (Exception ex) {
			//si se produce una excepción, vamos a borrar el archivo target, para que no quede un archivo corrupto y la aplicacion que hace uso de esta no se crea que es correcto
			if ((File.Exists(targetPdf))) {
				if (fileStream != null) {
					fileStream.Close();
				}                   
				File.Delete(targetPdf);
			}
			return ex.Message;
		}
	}

	 private static iText.Kernel.Geom.Rectangle getSignPositionRect(PdfDocument doc, int pageNumber, int increasedWidth, int increasedHeight) {           

		//cargamos la lista de matrices deposicionamiento (ctm) de las imagenes
		ImageExtractor imgs = new ImageExtractor();
		PdfCanvasProcessor pdfCanvasProcessor = new PdfCanvasProcessor(imgs);
		pdfCanvasProcessor.ProcessPageContent(doc.GetPage(pageNumber));
		List<Matrix> imageList = imgs.getImagesCtms();

		//identificamos la penultima imagen
		int imageIndex;
		if (imageList.Count > 1) { //we sign in the penultimate image
			imageIndex = imageList.Count - 2;
		} else if (imageList.Count == 0) { //is the page have no images
			return new iText.Kernel.Geom.Rectangle(50, 50, 150, 50);
		} else { //else, we sign in the first image
			imageIndex = 0;
		}

		//transform the matrix coordinates into the desired rect 
		float width = imageList[imageIndex].Get(Matrix.I11) + increasedWidth;
		float height = imageList[imageIndex].Get(Matrix.I22);
		float posX = imageList[imageIndex].Get(Matrix.I31) - (increasedWidth / 2);           
		float posY = imageList[imageIndex].Get(Matrix.I32) - (increasedHeight / 2);

		//por algun motivo segun la impresora utilizada para generar el pdf, la altura de la imagen puede sale en negativo (invertida)            
		if (height < 0) {
			//en este caso, como la imagen de la imagen parece estar invertida en el eje Y, tenemos que empezar a pintarla en la otra esquina de la imagen
			posY += height;
			height *= -1;                
		}        
		height += increasedHeight;
						   
		iText.Kernel.Geom.Rectangle rect = new iText.Kernel.Geom.Rectangle(posX, posY, width, height);
		return rect;         
	}    
}

//Esta clase estaba incluida en itext5, pero no esta en itext7
public class X509Certificate2Signature : IExternalSignature {
    private String hashAlgorithm;
    private String encryptionAlgorithm;
    private X509Certificate2 certificate;
    public X509Certificate2Signature(X509Certificate2 certificate, String hashAlgorithm) {
        if (!certificate.HasPrivateKey)
            throw new ArgumentException("No private key.");
        this.certificate = certificate;
        this.hashAlgorithm = DigestAlgorithms.GetDigest(DigestAlgorithms.GetAllowedDigest(hashAlgorithm));
        if (certificate.PrivateKey is RSACryptoServiceProvider)
            encryptionAlgorithm = "RSA";
        else if (certificate.PrivateKey is DSACryptoServiceProvider)
            encryptionAlgorithm = "DSA";
        else
            throw new ArgumentException("Unknown encryption algorithm " + certificate.PrivateKey);
    }
    public virtual byte[] Sign(byte[] message) {
        if (certificate.PrivateKey is RSACryptoServiceProvider) {
            RSACryptoServiceProvider rsa = (RSACryptoServiceProvider)certificate.PrivateKey;
            return rsa.SignData(message, hashAlgorithm);
        } else {
            DSACryptoServiceProvider dsa = (DSACryptoServiceProvider)certificate.PrivateKey;
            return dsa.SignData(message);
        }
    }
    public virtual String GetHashAlgorithm() {
        return hashAlgorithm;
    }
    public virtual String GetEncryptionAlgorithm() {
        return encryptionAlgorithm;
    }
}

//clase para extraer la informacion de posicion de las distintas imagenes de el documento
//los datos de las imagenes se cargaran al ejecutar ProccesPageContent (ver funcion getSignPositionRect)
public class ImageExtractor : IEventListener {
    private List<Matrix> ctms = new List<Matrix>();

    public void EventOccurred(IEventData data, EventType type) {
        if ((type != EventType.RENDER_IMAGE)) {
            return;
        }

        //si el evento es el renderizado de una imagen
        ImageRenderInfo img = ((ImageRenderInfo)(data));
        try {
            this.ctms.Add(img.GetImageCtm());
        } catch (IOException e) {

        }
    }
    public List<Matrix> getImagesCtms() { return ctms; }

    public ICollection<EventType> GetSupportedEvents() { return null; }

}