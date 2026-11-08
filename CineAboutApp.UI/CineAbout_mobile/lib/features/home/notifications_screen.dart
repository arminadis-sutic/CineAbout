import 'dart:async';

import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../core/models.dart';
import '../../core/repository.dart';
import '../../core/theme.dart';
import '../../core/widgets.dart';

class NotificationsScreen extends StatefulWidget {
  const NotificationsScreen({super.key});

  @override
  State<NotificationsScreen> createState() => _NotificationsScreenState();
}

class _NotificationsScreenState extends State<NotificationsScreen> {
  final _list = PagedListController();
  Timer? _timer;

  @override
  void initState() {
    super.initState();
    _timer = Timer.periodic(const Duration(seconds: 10), (_) => _list.refreshSilently());
  }

  @override
  void dispose() {
    _timer?.cancel();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final repo = context.read<Repository>();
    return Scaffold(
      appBar: AppBar(
        title: const Text('Notifications'),
        actions: [
          TextButton(
            onPressed: () async {
              if (await runAction(context, repo.markAllNotificationsRead)) _list.reload();
            },
            child: const Text('Mark all read'),
          ),
        ],
      ),
      body: PagedList<AppNotification>(
        controller: _list,
        emptyText: 'You are all caught up.',
        fetch: (page) => repo.notifications(page: page),
        itemBuilder: (context, n) => Card(
          child: ListTile(
            onTap: n.seen
                ? null
                : () async {
                    if (await runAction(context, () => repo.markNotificationRead(n.id))) {
                      _list.refreshSilently();
                    }
                  },
            leading: Icon(n.seen ? Icons.notifications_none : Icons.notifications_active,
                color: n.seen ? AppColors.muted : AppColors.red),
            title: Text(n.title,
                style: TextStyle(fontWeight: n.seen ? FontWeight.w500 : FontWeight.w800)),
            subtitle: Text('${n.body}\n${timeAgo(n.createdAt)}',
                style: const TextStyle(color: AppColors.muted)),
            isThreeLine: true,
          ),
        ),
      ),
    );
  }
}
