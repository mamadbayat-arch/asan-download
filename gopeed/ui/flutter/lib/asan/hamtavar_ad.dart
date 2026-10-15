// SPDX-License-Identifier: GPL-3.0-or-later
import 'dart:async';import 'dart:convert';import 'dart:io';
import 'package:dio/dio.dart';import 'package:flutter/material.dart';import 'package:path/path.dart' as p;import 'package:url_launcher/url_launcher.dart';
import '../util/util.dart';
class HamtavarAd extends StatefulWidget{const HamtavarAd({super.key});@override State<HamtavarAd> createState()=>_AdState();}
class _AdState extends State<HamtavarAd>{
 static const origin='https://downloader.hamtavar.ir';
 Map<String,dynamic> ad={'active':true,'title':'همراه روزهای منظم‌تر','ctaText':'آشنایی با همتوار','link':'https://hamtavar.ir','imageUrl':'/assets/hamtavar.png'};Timer? timer;
 @override void initState(){super.initState();_refresh();timer=Timer.periodic(const Duration(minutes:5),(_)=>_refresh());}
 File get _cache=>File(p.join(Util.getStorageDir(),'asan-ad.json'));
 Future<void> _refresh()async{try{final cached=jsonDecode(await _cache.readAsString());if(cached is Map&&mounted)setState(()=>ad=Map<String,dynamic>.from(cached));}catch(_){}
  try{final client=Dio(BaseOptions(connectTimeout:const Duration(seconds:4),receiveTimeout:const Duration(seconds:4),followRedirects:false));try{final result=await client.get('$origin/api/public/ad');if(result.data is Map){final next=Map<String,dynamic>.from(result.data);if(next['title'] is String&&next['link'] is String){await _cache.parent.create(recursive:true);await _cache.writeAsString(jsonEncode(next));if(mounted)setState(()=>ad=next);}}}finally{client.close();}}catch(_){}
 }
 @override void dispose(){timer?.cancel();super.dispose();}
 @override Widget build(BuildContext context){if(ad['active']==false)return const SizedBox.shrink();final image=ad['imageUrl']?.toString()??'';final safeImage=image.startsWith('/media/')||image=='/assets/hamtavar.png';return Card(child:Padding(padding:const EdgeInsets.all(10),child:Column(mainAxisSize:MainAxisSize.min,children:[
  if(safeImage)Image.network('$origin$image',height:28,errorBuilder:(_,__,___)=>Image.asset('assets/icon/hamtavar.png',height:28)) else Image.asset('assets/icon/hamtavar.png',height:28),
  const SizedBox(height:6),Text(ad['title']?.toString()??'همتوار',textAlign:TextAlign.center,maxLines:2,overflow:TextOverflow.ellipsis),
  TextButton(onPressed:(){final uri=Uri.tryParse(ad['link']?.toString()??'');if(uri!=null&&['https','http'].contains(uri.scheme)&&uri.userInfo.isEmpty)launchUrl(uri,mode:LaunchMode.externalApplication);},child:Text(ad['ctaText']?.toString()??'همتوار',textAlign:TextAlign.center)),
 ])));}
}
