// https://go.microsoft.com/fwlink/?Linkid=852423
window.addEventListener("load", () => {
  const uri = document.getElementById("qrCodeData").getAttribute('data-url');
  new QRCode(document.getElementById("qrCode"),
    {
      text: uri,
      width: 150,
      height: 150
    });
});