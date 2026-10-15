import 'package:flutter/material.dart';
import 'package:get/get.dart';

class AsanSidebar extends StatelessWidget {
  const AsanSidebar({super.key, required this.selectedIndex, required this.onSelect,
    required this.onHistory, required this.onReport, required this.footer});
  final int selectedIndex;
  final ValueChanged<int> onSelect;
  final VoidCallback onHistory;
  final VoidCallback onReport;
  final Widget footer;

  @override
  Widget build(BuildContext context) => SizedBox(width: 220,
    child: Material(color: Theme.of(context).colorScheme.surface,
      child: SafeArea(child: ListView(padding: const EdgeInsets.all(16), children: [
        Padding(padding: const EdgeInsets.symmetric(vertical: 16),
          child: Image.asset('assets/icon/asan-wordmark.png', width: 156, height: 52, fit: BoxFit.contain,
            color: Theme.of(context).brightness == Brightness.dark ? Theme.of(context).colorScheme.onSurface : null,
            colorBlendMode: BlendMode.srcIn)),
        const SizedBox(height: 16),
        for (final entry in [
          (Icons.download_rounded, 'task'), (Icons.extension_rounded, 'extensions'), (Icons.settings_rounded, 'setting'),
        ].asMap().entries)
          AsanNavigationTile(icon: entry.value.$1, label: entry.value.$2.tr,
            selected: selectedIndex == entry.key, onTap: () => onSelect(entry.key)),
        const Padding(padding: EdgeInsets.symmetric(vertical: 12), child: Divider()),
        AsanNavigationTile(icon: Icons.history_rounded, label: 'دانلودهای نسخهٔ قبلی', onTap: onHistory),
        AsanNavigationTile(icon: Icons.bug_report_outlined, label: 'گزارش مشکل', onTap: onReport),
        const SizedBox(height: 24), footer,
      ]))));
}

class AsanNavigationTile extends StatelessWidget {
  const AsanNavigationTile({super.key, required this.icon, required this.label,
    required this.onTap, this.selected = false});
  final IconData icon;
  final String label;
  final VoidCallback onTap;
  final bool selected;
  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.only(bottom: 8),
    child: ListTile(shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
      minLeadingWidth: 24, horizontalTitleGap: 12, minTileHeight: 56,
      selected: selected, selectedTileColor: Theme.of(context).colorScheme.primaryContainer,
      selectedColor: Theme.of(context).colorScheme.onPrimaryContainer,
      leading: Icon(icon, size: 22),
      title: Text(label, textAlign: TextAlign.start, maxLines: 2, overflow: TextOverflow.ellipsis,
        style: Theme.of(context).textTheme.bodyMedium?.copyWith(fontWeight: selected ? FontWeight.w700 : FontWeight.w400)),
      onTap: onTap));
}
