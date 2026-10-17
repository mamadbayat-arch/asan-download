// SPDX-License-Identifier: GPL-3.0-or-later
import 'dart:async';

// A browser download must stay recoverable until the origin has been resolved.
// Queueing an unresolved task is not proof the app can receive that file.
Future<Map<String, dynamic>> handoffBrowserDownload({
  required Future<bool> Function() confirm,
  required Future<String> Function() prepare,
  required Future<void> Function(String) create,
  Duration prepareTimeout = const Duration(seconds: 25),
}) async {
  try {
    if (!await confirm()) return {'accepted': false, 'cancelled': true};
    final id = await prepare().timeout(prepareTimeout);
    await create(id);
    return {'accepted': true};
  } catch (_) {
    return {'accepted': false, 'reason': 'browser_required'};
  }
}
