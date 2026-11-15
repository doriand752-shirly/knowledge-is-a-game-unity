import 'package:flutter_test/flutter_test.dart';
import 'package:kiag_player/main.dart';

void main() {
  testWidgets('Join page shows brand', (tester) async {
    await tester.pumpWidget(const KiagApp());
    expect(find.textContaining('Knowledge'), findsOneWidget);
  });
}
