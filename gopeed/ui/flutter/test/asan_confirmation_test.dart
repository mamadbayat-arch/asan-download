import 'package:flutter_test/flutter_test.dart';
import 'package:gopeed/asan/browser_confirmation.dart';
void main(){test('browser links require HTTP(S), a host and no embedded credentials',(){expect(validBrowserUrl('https://example.com/file.zip'),isTrue);expect(validBrowserUrl('file:///C:/data'),isFalse);expect(validBrowserUrl('https://user:pass@example.com/file'),isFalse);expect(validBrowserUrl('https:///file'),isFalse);});}
