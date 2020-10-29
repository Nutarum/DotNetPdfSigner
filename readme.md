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
* Desde la consola de comandos, ejecutar "./PdfSigner.exe input.pdf output.pdf 12345678A 1"
	* Parametros:		
		* Ruta del pdf a firmar
		* Ruta donde generar el pdf firmado
		* DNI con letra
			* Opcion 1: indice_imagen (esto es el indice de la imagen en la que queremos firmar (la idea es generar el pdf colocando una imagen en blanco donde vayamos a querer firmar )
			* Opcion 2: posicion_x posicion_y ancho alto
	
```
Por estar utilizando la version del framework 4.5, este programa no funciona en windows xp
```