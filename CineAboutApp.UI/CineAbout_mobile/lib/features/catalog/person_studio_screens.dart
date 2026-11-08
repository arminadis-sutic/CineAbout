import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../core/models.dart';
import '../../core/repository.dart';
import '../../core/theme.dart';
import '../../core/widgets.dart';
import '../movie/movie_cards.dart';

class PersonScreen extends StatelessWidget {
  const PersonScreen({super.key, required this.personId});

  final int personId;

  @override
  Widget build(BuildContext context) {
    final repo = context.read<Repository>();
    return Scaffold(
      appBar: AppBar(title: const Text('Person')),
      body: AsyncData<(PersonInfo, List<MovieCard>)>(
        load: () async => (await repo.person(personId), await repo.personMovies(personId)),
        builder: (context, data, reload) {
          final (p, movies) = data;
          return RefreshIndicator(
            onRefresh: reload,
            child: ListView(padding: const EdgeInsets.all(16), children: [
              Center(child: Avatar(username: p.name, url: p.photoUrl, radius: 56)),
              const SizedBox(height: 12),
              Center(child: Text(p.name, textAlign: TextAlign.center, style: const TextStyle(fontSize: 24, fontWeight: FontWeight.w900))),
              const SizedBox(height: 4),
              Center(
                child: Text(
                  [p.role, if (p.birthDate != null) 'Born ${formatDate(p.birthDate!)}'].join('  ·  '),
                  style: const TextStyle(color: AppColors.muted),
                ),
              ),
              if (p.biography.isNotEmpty) ...[
                const SizedBox(height: 16),
                Text(p.biography, style: const TextStyle(height: 1.4)),
              ],
              const SizedBox(height: 16),
              Text('Titles (${movies.length})', style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w800)),
              const SizedBox(height: 8),
              if (movies.isEmpty) const Text('No titles yet.', style: TextStyle(color: AppColors.muted)),
              for (final m in movies) MovieTile(movie: m, subtitle: m.role),
            ]),
          );
        },
      ),
    );
  }
}

class StudioScreen extends StatelessWidget {
  const StudioScreen({super.key, required this.studioId});

  final int studioId;

  @override
  Widget build(BuildContext context) {
    final repo = context.read<Repository>();
    return Scaffold(
      appBar: AppBar(title: const Text('Studio')),
      body: AsyncData<(StudioInfo, List<MovieCard>)>(
        load: () async => (await repo.studio(studioId), await repo.studioMovies(studioId)),
        builder: (context, data, reload) {
          final (s, movies) = data;
          return RefreshIndicator(
            onRefresh: reload,
            child: ListView(padding: const EdgeInsets.all(16), children: [
              Center(child: Poster(url: s.logoUrl, title: s.name, width: 140, height: 100, radius: 12)),
              const SizedBox(height: 12),
              Center(child: Text(s.name, textAlign: TextAlign.center, style: const TextStyle(fontSize: 24, fontWeight: FontWeight.w900))),
              const SizedBox(height: 4),
              Center(
                child: Text(
                  [if (s.countryName.isNotEmpty) s.countryName, if (s.foundedYear > 0) 'Founded ${s.foundedYear}'].join('  ·  '),
                  style: const TextStyle(color: AppColors.muted),
                ),
              ),
              if (s.description.isNotEmpty) ...[
                const SizedBox(height: 16),
                Text(s.description, style: const TextStyle(height: 1.4)),
              ],
              const SizedBox(height: 16),
              Text('Titles (${movies.length})', style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w800)),
              const SizedBox(height: 8),
              if (movies.isEmpty) const Text('No titles yet.', style: TextStyle(color: AppColors.muted)),
              for (final m in movies) MovieTile(movie: m),
            ]),
          );
        },
      ),
    );
  }
}
