import 'package:flutter/material.dart';

/// Keep Latin paths and filenames intact, aligned with the surrounding UI.
class AsanTechnicalText extends StatelessWidget {
  const AsanTechnicalText(this.value, {super.key, this.style, this.maxLines = 1});
  final String value;
  final TextStyle? style;
  final int maxLines;
  @override
  Widget build(BuildContext context) => Text(value,
    textDirection: TextDirection.ltr,
    textAlign: Directionality.of(context) == TextDirection.rtl ? TextAlign.right : TextAlign.left,
    maxLines: maxLines, overflow: TextOverflow.ellipsis, style: style);
}
