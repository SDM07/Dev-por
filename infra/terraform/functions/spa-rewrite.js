// CloudFront Function (viewer-request) del comportamiento por defecto (S3).
// Las rutas del SPA (/apis/cuentas, /admin) no existen en S3: se reescriben a /index.html.
// Las peticiones a archivos (con extensión) pasan sin cambios. /api/* nunca llega aquí.
function handler(event) {
  var request = event.request;
  var uri = request.uri;
  var lastSegment = uri.substring(uri.lastIndexOf('/') + 1);
  if (lastSegment.indexOf('.') === -1) {
    request.uri = '/index.html';
  }
  return request;
}
