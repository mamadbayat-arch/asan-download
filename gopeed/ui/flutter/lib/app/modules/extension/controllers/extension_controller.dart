import 'dart:async';
import 'dart:collection';

import 'package:get/get.dart';

import '../../../../api/api.dart';
import '../../../../api/extension_catalog.dart';
import '../../../../api/model/extension.dart';
import '../../../../api/model/install_extension.dart';
import '../../../../api/model/store_extension.dart';
import '../../../../api/model/switch_extension.dart';

enum ExtensionListFilter {
  market,
  installed,
}

class ExtensionListItem {
  final Extension? installed;
  final StoreExtension? store;

  const ExtensionListItem({this.installed, this.store});

  bool get isInstalled => installed != null;

  String get id => installed?.identity ?? store!.id;

  String get title => installed?.title ?? store?.title ?? '';

  String get author => installed?.author ?? store?.author ?? '';

  String get description => installed?.description ?? store?.description ?? '';

  String get version => installed?.version ?? store?.version ?? '0.0.0';

  String? get homepage {
    final installedHomepage = installed?.homepage;
    if (installedHomepage != null && installedHomepage.isNotEmpty) {
      return installedHomepage;
    }
    return store?.homepage;
  }

  String? get repoUrl {
    final installedRepo = installed?.repository?.url;
    if (installedRepo != null && installedRepo.isNotEmpty) {
      return installedRepo;
    }
    return store?.repoUrl;
  }

  int get stars => store?.stars ?? 0;

  int get installCount => store?.installCount ?? 0;

  String? get icon => store?.icon;
}

class ExtensionController extends GetxController {
  ExtensionController({Future<StoreExtensionPage> Function(int, int, StoreExtensionSort, String)? loadCatalog})
      : _loadCatalog = loadCatalog ?? ((page, limit, sort, query) => ExtensionCatalog.instance.getExtensions(page: page, limit: limit, sort: sort, query: query));
  final Future<StoreExtensionPage> Function(int, int, StoreExtensionSort, String) _loadCatalog;
  final storeError = ''.obs;
  int _storeGeneration = 0;

  static const manualInstallBusyKey = '__manual_install__';

  final installedExtensions = <Extension>[].obs;
  final updateFlags = <String, String>{}.obs;

  final storeExtensions = <StoreExtension>[].obs;
  final storePagination = Rxn<StorePagination>();
  final storeQuery = ''.obs;
  final storeSort = StoreExtensionSort.stars.obs;

  final listFilter = ExtensionListFilter.market.obs;

  final loadingInstalled = false.obs;
  final loadingStore = false.obs;
  final loadingMoreStore = false.obs;
  final busyExtensionIds = <String>{}.obs;
  final showInstallTools = false.obs;

  final devMode = false.obs;
  var _devModeCount = 0;

  bool pendingInstallHandled = false;

  UnmodifiableMapView<String, Extension> get installedMap =>
      UnmodifiableMapView(
          {for (final ext in installedExtensions) ext.identity: ext});

  UnmodifiableMapView<String, StoreExtension> get storeMap =>
      UnmodifiableMapView({for (final ext in storeExtensions) ext.id: ext});

  List<ExtensionListItem> get displayItems {
    if (listFilter.value == ExtensionListFilter.installed) {
      return installedExtensions
          .map((ext) =>
              ExtensionListItem(installed: ext, store: storeMap[ext.identity]))
          .toList();
    }

    return storeExtensions
        .map((ext) =>
            ExtensionListItem(installed: installedMap[ext.id], store: ext))
        .toList();
  }

  @override
  Future<void> onInit() async {
    super.onInit();
    await loadInitialData();
  }

  Future<void> loadInitialData() async {
    await Future.wait([loadInstalled(refreshUpdates: true), refreshStore()]);
  }

  Future<void> loadInstalled({bool refreshUpdates = false}) async {
    loadingInstalled.value = true;
    try {
      installedExtensions.value = await getExtensions();
      if (refreshUpdates) {
        await checkUpdate();
      }
    } finally {
      loadingInstalled.value = false;
    }
  }

  Future<void> checkUpdate() async {
    final nextFlags = <String, String>{};
    for (final ext in installedExtensions) {
      try {
        final resp = await upgradeCheckExtension(ext.identity);
        if (resp.newVersion.isNotEmpty) {
          nextFlags[ext.identity] = resp.newVersion;
        }
      } catch (_) {
        // Ignore single extension check failures to avoid breaking the whole list.
      }
    }
    updateFlags.assignAll(nextFlags);
  }

  Future<void> refreshStore() async {
    final generation = ++_storeGeneration;
    loadingStore.value = true;
    storeError.value = '';
    try {
      final page = await _loadCatalog(1, 20, storeSort.value, storeQuery.value);
      if (generation != _storeGeneration) return;
      storeExtensions.assignAll(page.data);
      storePagination.value = page.pagination;
    } catch (_) {
      if (generation == _storeGeneration) storeError.value = 'extensionCatalogUnavailable'.tr;
    } finally {
      if (generation == _storeGeneration) loadingStore.value = false;
    }
  }

  Future<void> loadMoreStore() async {
    final pagination = storePagination.value;
    if (pagination == null || !pagination.hasNext || loadingMoreStore.value || loadingStore.value) return;
    final generation = _storeGeneration;
    loadingMoreStore.value = true;
    try {
      final page = await _loadCatalog(pagination.page + 1, pagination.limit, storeSort.value, storeQuery.value);
      if (generation != _storeGeneration) return;
      storeExtensions.addAll(page.data);
      storePagination.value = page.pagination;
      storeError.value = '';
    } catch (_) {
      if (generation == _storeGeneration) storeError.value = 'extensionCatalogUnavailable'.tr;
    } finally {
      loadingMoreStore.value = false;
    }
  }

  Future<void> searchStore(String query) async {
    storeQuery.value = query.trim();
    await refreshStore();
  }

  Future<void> changeSort(StoreExtensionSort sort) async {
    if (storeSort.value == sort) {
      await refreshStore();
      return;
    }
    storeSort.value = sort;
    await refreshStore();
  }

  void changeFilter(ExtensionListFilter filter) {
    listFilter.value = filter;
  }

  void toggleInstallTools() {
    showInstallTools.value = !showInstallTools.value;
  }

  Extension? findInstalled(StoreExtension extension) {
    return installedMap[extension.id];
  }

  bool canUpdateFromStore(StoreExtension extension) {
    final installed = findInstalled(extension);
    if (installed == null) return false;
    return _compareVersion(extension.version, installed.version) > 0;
  }

  bool canUpdateItem(ExtensionListItem item) {
    if (item.installed == null) return false;
    if (listFilter.value == ExtensionListFilter.market && item.store != null) {
      return canUpdateFromStore(item.store!);
    }
    return updateFlags.containsKey(item.installed!.identity);
  }

  Future<void> installFromStore(StoreExtension extension) async {
    await _runBusy(extension.id, () async {
      final installUrl = (extension.directory ?? '').trim().isEmpty
          ? extension.repoUrl
          : '${extension.repoUrl}#${extension.directory!.trim()}';

      final installedId =
          await installExtension(InstallExtension(url: installUrl));
      final statsId = installedId.isNotEmpty ? installedId : extension.id;
      await loadInstalled(refreshUpdates: false);
      _backgroundCheckUpdate();
      _bumpStoreInstallCount(statsId);
    });
  }

  Future<void> installFromUrl(String url,
      {bool devInstall = false, String? statsId}) async {
    await _runBusy(manualInstallBusyKey, () async {
      final installedId = await installExtension(
          InstallExtension(devMode: devInstall, url: url));
      await loadInstalled(refreshUpdates: false);
      _backgroundCheckUpdate();
      if (installedId.isNotEmpty) {
        _bumpStoreInstallCount(installedId);
      } else if (statsId != null && statsId.isNotEmpty) {
        _bumpStoreInstallCount(statsId);
      }
    });
  }

  Future<void> toggleExtension(Extension extension, bool enabled) async {
    await _runBusy(extension.identity, () async {
      await switchExtension(
          extension.identity, SwitchExtension(status: enabled));
      await loadInstalled(refreshUpdates: false);
    });
  }

  Future<void> removeExtension(Extension extension) async {
    await _runBusy(extension.identity, () async {
      await deleteExtension(extension.identity);
      await loadInstalled(refreshUpdates: false);
      updateFlags.remove(extension.identity);
    });
  }

  Future<void> upgradeExtension(Extension extension) async {
    await _runBusy(extension.identity, () async {
      await updateExtension(extension.identity);
      await loadInstalled(refreshUpdates: false);
      _backgroundCheckUpdate();
      _bumpStoreInstallCount(extension.identity);
    });
  }

  void tryOpenDevMode() {
    if (_devModeCount == 0) {
      Future.delayed(const Duration(seconds: 2), () {
        if (devMode.value) return;
        devMode.value = false;
        _devModeCount = 0;
      });
    }
    _devModeCount++;
    if (_devModeCount >= 5) {
      devMode.value = true;
    }
  }

  static int _compareVersion(String a, String b) {
    final aNums = _toVersionNumbers(a);
    final bNums = _toVersionNumbers(b);
    final maxLen = aNums.length > bNums.length ? aNums.length : bNums.length;

    for (var i = 0; i < maxLen; i++) {
      final left = i < aNums.length ? aNums[i] : 0;
      final right = i < bNums.length ? bNums[i] : 0;
      if (left > right) return 1;
      if (left < right) return -1;
    }
    return 0;
  }

  static List<int> _toVersionNumbers(String version) {
    return version
        .split(RegExp(r'[^0-9]+'))
        .where((part) => part.isNotEmpty)
        .map((part) => int.tryParse(part) ?? 0)
        .toList();
  }

  Future<void> _runBusy(String id, Future<void> Function() action) async {
    if (busyExtensionIds.contains(id)) return;
    busyExtensionIds.add(id);
    try {
      await action();
    } finally {
      busyExtensionIds.remove(id);
    }
  }

  void _backgroundCheckUpdate() {
    unawaited(checkUpdate());
  }

  void _bumpStoreInstallCount(String id) {
    final index = storeExtensions.indexWhere((e) => e.id == id);
    if (index < 0) return;
    final ext = storeExtensions[index];
    storeExtensions[index] = StoreExtension(
      id: ext.id,
      repoFullName: ext.repoFullName,
      repoUrl: ext.repoUrl,
      directory: ext.directory,
      commitSha: ext.commitSha,
      name: ext.name,
      author: ext.author,
      title: ext.title,
      description: ext.description,
      icon: ext.icon,
      version: ext.version,
      homepage: ext.homepage,
      readme: ext.readme,
      installCount: ext.installCount + 1,
      stars: ext.stars,
      topics: ext.topics,
      createdAt: ext.createdAt,
      updatedAt: ext.updatedAt,
    );
  }
}
