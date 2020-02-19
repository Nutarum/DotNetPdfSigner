# PdfSigner

Aplicación C# que firma un PDF utilizando una firma digital registrada en windows

## Registrar la firma en Windows
* Obtener el certificado digital
* Internet explorer -> Configuración -> Opciones de internet -> Contenido -> Certificados -> Importar

## Preparar el programa 
* Generar el .exe (para que el exe funcione correctamente, todos los dlls que se han generado con el tienen que mantenerse en su misma carpeta)
* Crear una carpeta llamada "imagenes" en la misma ubiación que el .exe
* Guardar una imagen de la firma, con el nombre DNI+Letra.jpg (12345678A.jpg) dentro de la carpeta imagenes

## Utilización del programa
* Desde la consola de comandos, ejecutar "./PdfSigner.exe 12345678A.jpg input.pdf output.pdf"
	* Parametros obligatorios:
		* DNI con letra
		* Ruta del pdf a firmar
		* Ruta donde generar el pdf firmado
	* Parametros opcionales
		* Hacer la firma visible (para que la firma sea visible, el 4º parametro debe ser "true", si no existe 4º parametro, la firma siempre será visible)
		* Anchura aumentada (numero, en pixels, de cuanto queremos aumentar el tamaño de la firma en anchura (Los 2 parametos de tamaño, funcionan solo cuando firmamos sobre una imagen en el pdf))
		* Altura aumentada (numero, en pixels, de cuanto queremos aumentar el tamaño de la firma en altura)  
			&ensp;*estos 2 ultimos parametros, solo funcionan si el pdf tiene alguna imagen (sobre la que se colocará la firma)
	
## Información extra
> en el caso de estar utilizando el programa en el modo de firma visible, la firma aparecerá, por defecto, sobre la penúltima imagen de la ultima pagina fichero (la última en caso de ser la única, o abajo a la izquierda en caso de no haber ninguna)  
> para aprovechar esto, la idea es generar el pdf colocando una imagen en blanco donde vayamos a querer firmar (o modifica el código para firmar donde te apetezca, claro...)
```
Por estar utilizando la version del framework 4.5, este programa no funciona en windows xp
```