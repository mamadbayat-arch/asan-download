import 'package:flutter/material.dart';

class GopeedTheme {
  static const fontFamily = 'Kalameh';
  static final light = _build(Brightness.light);
  static final dark = _build(Brightness.dark);

  static ThemeData _build(Brightness brightness) {
    final generated = ColorScheme.fromSeed(seedColor: const Color(0xFF107C65), brightness: brightness);
    final scheme = generated.copyWith(secondary: generated.primary, onSecondary: generated.onPrimary);
    final base = ThemeData(useMaterial3: true, brightness: brightness, colorScheme: scheme,
      fontFamily: fontFamily, fontFamilyFallback: const ['Segoe UI', 'sans-serif']);
    TextStyle? type(TextStyle? style, {double? size, bool bold = false}) =>
        style?.copyWith(fontFamily: fontFamily, fontSize: size, height: 1.55,
          letterSpacing: 0, leadingDistribution: TextLeadingDistribution.even,
          fontWeight: bold ? FontWeight.w700 : FontWeight.w400);
    final text = base.textTheme;
    final typography = TextTheme(
      displayLarge: type(text.displayLarge), displayMedium: type(text.displayMedium),
      displaySmall: type(text.displaySmall), headlineLarge: type(text.headlineLarge, bold: true),
      headlineMedium: type(text.headlineMedium, bold: true), headlineSmall: type(text.headlineSmall, bold: true),
      titleLarge: type(text.titleLarge, size: 22, bold: true), titleMedium: type(text.titleMedium, size: 16, bold: true),
      titleSmall: type(text.titleSmall, size: 14, bold: true), bodyLarge: type(text.bodyLarge, size: 16),
      bodyMedium: type(text.bodyMedium, size: 14), bodySmall: type(text.bodySmall, size: 12),
      labelLarge: type(text.labelLarge, size: 14, bold: true),
      labelMedium: type(text.labelMedium, size: 13), labelSmall: type(text.labelSmall, size: 12));
    const shape = RoundedRectangleBorder(borderRadius: BorderRadius.all(Radius.circular(12)));
    return base.copyWith(textTheme: typography, primaryTextTheme: typography,
      scaffoldBackgroundColor: brightness == Brightness.light ? const Color(0xFFF5F8F7) : const Color(0xFF111B19),
      appBarTheme: AppBarTheme(centerTitle: false, elevation: 0,
        backgroundColor: scheme.surface, titleTextStyle: typography.titleLarge?.copyWith(color: scheme.onSurface)),
      textButtonTheme: TextButtonThemeData(style: TextButton.styleFrom(minimumSize: const Size(48, 44),
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10), textStyle: typography.labelLarge, shape: shape, alignment: Alignment.center)),
      filledButtonTheme: FilledButtonThemeData(style: FilledButton.styleFrom(minimumSize: const Size(48, 44),
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10), textStyle: typography.labelLarge, shape: shape)),
      elevatedButtonTheme: ElevatedButtonThemeData(style: ElevatedButton.styleFrom(minimumSize: const Size(48, 44),
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10), textStyle: typography.labelLarge, shape: shape, elevation: 0)),
      outlinedButtonTheme: OutlinedButtonThemeData(style: OutlinedButton.styleFrom(minimumSize: const Size(48, 44),
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10), textStyle: typography.labelLarge, shape: shape)),
      inputDecorationTheme: InputDecorationTheme(filled: true, fillColor: scheme.surface,
        contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
        border: OutlineInputBorder(borderRadius: BorderRadius.circular(12)),
        labelStyle: typography.bodyMedium, hintStyle: typography.bodyMedium?.copyWith(color: scheme.onSurfaceVariant)),
      listTileTheme: ListTileThemeData(contentPadding: const EdgeInsetsDirectional.symmetric(horizontal: 16, vertical: 4),
        minVerticalPadding: 10, titleTextStyle: typography.bodyLarge?.copyWith(color: scheme.onSurface),
        subtitleTextStyle: typography.bodySmall?.copyWith(color: scheme.onSurfaceVariant)),
      tabBarTheme: TabBarThemeData(labelStyle: typography.labelLarge, unselectedLabelStyle: typography.labelLarge));
  }
}
