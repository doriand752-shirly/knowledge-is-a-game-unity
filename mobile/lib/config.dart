/// Backend Colyseus (VPS). Override with --dart-define=KIAG_API=...
const String kDefaultApiHttp = String.fromEnvironment(
  'KIAG_API',
  defaultValue: 'http://85.17.145.134:2567',
);

const String kBrand = 'Knowledge is a Game';

String wsFromHttp(String http) {
  if (http.startsWith('https://')) return 'wss://${http.substring(8)}';
  if (http.startsWith('http://')) return 'ws://${http.substring(7)}';
  return 'ws://$http';
}
