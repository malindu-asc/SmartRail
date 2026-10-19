import 'package:flutter/material.dart';
import 'package:flutter_svg/flutter_svg.dart';

/// The icons of the Figma file, as the exact vector files exported from it. Each one is a single colour, so it is tinted with the
/// colour the caller gives (the files carry their own colour only as a default).
abstract final class AppIcons {
  static const access = 'access';
  static const alert = 'alert';
  static const arrows = 'arrows';
  static const back = 'back';
  static const chevron = 'chevron';
  static const filter = 'filter';
  static const bell = 'bell';
  static const check = 'check';
  static const clock = 'clock';
  static const fork = 'fork';
  static const globe = 'globe';
  static const rightSmall = 'right_small';
  static const type = 'type';
  static const bellRow = 'bell_row';
  static const download = 'download';
  static const trash = 'trash';
  static const phone = 'phone';
  static const logout = 'logout';
  static const info = 'info';
  static const history = 'history';
  static const home = 'home';
  static const pin = 'pin';
  static const right = 'right';
  static const route = 'route';
  static const swap = 'swap';
  static const swapChip = 'swap_chip';
  static const swapSmall = 'swap_small';
  static const ticket = 'ticket';
  static const train = 'train';
  static const user = 'user';
  static const wifiOff = 'wifioff';
  static const xc = 'xc';
}

class SvgIcon extends StatelessWidget {
  const SvgIcon(this.name, {super.key, this.size = 24, this.color});

  final String name;
  final double size;

  /// Null keeps the colour of the file.
  final Color? color;

  @override
  Widget build(BuildContext context) => SvgPicture.asset(
    'assets/icons/$name.svg',
    package: 'smartrail_design_system',
    width: size,
    height: size,
    colorFilter: color == null
        ? null
        : ColorFilter.mode(color!, BlendMode.srcIn),
  );
}
