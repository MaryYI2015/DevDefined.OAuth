REM Creating certificates needs an an implementation of OpenSSL, see http://www.openssl.org/ for more information
REM A Win32 compile can be found here: http://www.slproweb.com/products/Win32OpenSSL.html

@echo off
openssl.exe genrsa –out privatekey.pem 1024
openssl.exe req –newkey rsa:1024 –x509 –key privatekey.pem –out publickey.cer –days 365
openssl.exe pkcs12 –export –out public_privatekey.pfx –inkey privatekey.pem –in publickey.cer
