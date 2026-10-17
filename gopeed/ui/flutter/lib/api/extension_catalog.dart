// SPDX-License-Identifier: GPL-3.0-or-later
import 'dart:convert';
import 'package:dio/dio.dart';
import 'model/store_extension.dart';

// A product-owned catalogue. Installing a repository never sends install
// statistics to another project's service. Authors keep their own identities.
class ExtensionCatalog {
  ExtensionCatalog._();
  static final instance = ExtensionCatalog._();
  final _client = Dio(BaseOptions(connectTimeout: const Duration(seconds: 5),
    receiveTimeout: const Duration(seconds: 8), responseType: ResponseType.plain));

  Future<StoreExtensionPage> getExtensions({int page = 1, int limit = 20,
    StoreExtensionSort sort = StoreExtensionSort.stars, String? query}) async {
    final response = await _client.get<String>('https://downloader.hamtavar.ir/extensions/catalog.json');
    final json = jsonDecode(response.data ?? '') as Map<String, dynamic>;
    if (json['schema'] != 1 || json['engine'] != '1.9') throw const FormatException('Unsupported catalogue');
    final items = (json['data'] as List).map((row) => StoreExtension.fromJson(Map<String, dynamic>.from(row))).toList();
    return selectPage(items, page: page, limit: limit, sort: sort, query: query);
  }

  static StoreExtensionPage selectPage(List<StoreExtension> entries, {int page = 1, int limit = 20,
    StoreExtensionSort sort = StoreExtensionSort.stars, String? query}) {
    final needle = (query ?? '').trim().toLowerCase();
    final items = entries.where((item) {
      final uri = Uri.tryParse(item.repoUrl);
      return uri != null && uri.scheme == 'https' && uri.host == 'github.com' && uri.userInfo.isEmpty &&
        (needle.isEmpty || '${item.title} ${item.description} ${item.author}'.toLowerCase().contains(needle));
    }).toList();
    items.sort((a, b) => switch(sort) {
      StoreExtensionSort.stars => b.stars.compareTo(a.stars),
      StoreExtensionSort.installs => b.installCount.compareTo(a.installCount),
      StoreExtensionSort.updated => (b.updatedAt ?? DateTime(1970)).compareTo(a.updatedAt ?? DateTime(1970)),
    });
    final safeLimit = limit.clamp(1, 100).toInt(), total = items.length;
    final pages = ((total / safeLimit).ceil()).clamp(1, 1000000).toInt();
    final safePage = page.clamp(1, pages).toInt(), start = (safePage - 1) * safeLimit;
    return StoreExtensionPage(data: items.skip(start).take(safeLimit).toList(), pagination: StorePagination(
      page: safePage, limit: safeLimit, total: total, totalPages: pages,
      hasNext: safePage < pages, hasPrev: safePage > 1));
  }
}
