import 'dart:async';
import 'package:flutter_test/flutter_test.dart';
import 'package:get/get.dart';
import 'package:gopeed/api/extension_catalog.dart';
import 'package:gopeed/asan/browser_handoff.dart';
import 'package:gopeed/api/model/store_extension.dart';
import 'package:gopeed/app/modules/extension/controllers/extension_controller.dart';
import 'package:gopeed/util/updater.dart';

StoreExtension entry(String id, {int stars = 0, String? url}) => StoreExtension(
  id: id, repoFullName: 'author/$id', repoUrl: url ?? 'https://github.com/author/$id',
  name: id, author: 'author', title: id, description: '', version: '1.0.0',
  installCount: 0, stars: stars, topics: const []);
StoreExtensionPage page(String id) => ExtensionCatalog.selectPage([entry(id)]);

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();
  tearDown(() => Get.reset());
  test('origin failure resumes browser instead of cancelling it for an unresolved task', () async {
    var created = false;
    final reply = await handoffBrowserDownload(confirm: () async => true,
      prepare: () async => throw Exception('HTTP 403'), create: (_) async { created = true; });
    expect(reply['accepted'], false); expect(reply['reason'], 'browser_required'); expect(created, false);
  });
  test('browser cancel never resolves or creates a download', () async {
    final reply = await handoffBrowserDownload(confirm: () async => false,
      prepare: () async => throw StateError('must not resolve'), create: (_) async => fail('must not create'));
    expect(reply['cancelled'], true);
  });
  test('browser waits for successful origin resolution and creation', () async {
    final pending = Completer<String>(); var created = false, answered = false;
    final transfer = handoffBrowserDownload(confirm: () async => true,
      prepare: () => pending.future, create: (id) async { expect(id, 'resolved'); created = true; });
    transfer.then((_) => answered = true);
    await Future<void>.delayed(Duration.zero); expect(answered, false);
    pending.complete('resolved'); expect((await transfer)['accepted'], true); expect(created, true);
  });
  test('slow origin returns browser fallback and creates no delayed task', () async {
    final pending = Completer<String>(); var created = false;
    final reply = await handoffBrowserDownload(confirm: () async => true,
      prepare: () => pending.future, create: (_) async { created = true; }, prepareTimeout: Duration.zero);
    expect(reply['accepted'], false);
    pending.complete('late'); await Future<void>.delayed(Duration.zero); expect(created, false);
  });
  test('release feed rejects another app, downgrades and malformed versions', () {
    final feed = <String,dynamic>{'application':'ir.hamtavar.asandownload2',
      'platform':'windows-x64','channel':'preview','version':'2.0.10','notes':'تغییرات'};
    expect(parseAsanRelease(feed, '2.0.2')!.version, '2.0.10');
    expect(parseAsanRelease({...feed,'version':'1.99.99'}, '2.0.2'), isNull);
    expect(parseAsanRelease({...feed,'version':'2.0.2'}, '2.0.2'), isNull);
    expect(parseAsanRelease({...feed,'application':'other'}, '2.0.2'), isNull);
    expect(parseAsanRelease({...feed,'platform':'linux-x64'}, '2.0.2'), isNull);
    expect(() => parseAsanRelease({...feed,'version':'latest'}, '2.0.2'), throwsFormatException);
  });
  test('catalogue filters unsafe repositories and paginates actual matches', () {
    final result = ExtensionCatalog.selectPage([entry('a'),entry('b',stars:20),
      entry('bad',url:'http://github.com/author/bad')],limit:1);
    expect(result.data.single.id, 'b'); expect(result.pagination.total, 2);
    expect(result.pagination.hasNext, isTrue);
    expect(ExtensionCatalog.selectPage([entry('a')],query:'unmatched').data, isEmpty);
  });
  test('catalogue network error is recoverable and preserves loaded entries', () async {
    var fail = true;
    final controller = ExtensionController(loadCatalog: (_,__,___,____) async {
      if(fail) throw Exception('offline'); return page('new');
    });
    controller.storeExtensions.add(entry('previous'));
    await controller.refreshStore();
    expect(controller.storeError.value, isNotEmpty);
    expect(controller.storeExtensions.single.id, 'previous');
    expect(controller.loadingStore.value, isFalse);
    fail = false; await controller.refreshStore();
    expect(controller.storeError.value, isEmpty);
    expect(controller.storeExtensions.single.id, 'new');
  });
  test('slower previous search cannot replace the latest results', () async {
    final first = Completer<StoreExtensionPage>(), second = Completer<StoreExtensionPage>();
    var calls = 0;
    final controller = ExtensionController(loadCatalog: (_,__,___,____) => ++calls == 1 ? first.future : second.future);
    final old = controller.searchStore('old'), fresh = controller.searchStore('fresh');
    second.complete(page('fresh')); await fresh;
    first.complete(page('old')); await old;
    expect(controller.storeExtensions.single.id, 'fresh');
    expect(controller.loadingStore.value, isFalse);
  });
}
