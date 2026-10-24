/// The last good answer to a public read, with the moment it was fetched.
///
/// Used for `ETag` revalidation and to show stale data instead of nothing (REQ-PAX-005, REQ-NFR-006).
class CachedResponse {
  const CachedResponse({
    required this.body,
    required this.etag,
    required this.fetchedAt,
  });

  final Object? body;
  final String? etag;

  /// When the server last confirmed or sent this body. A `304` does not change it.
  final DateTime fetchedAt;
}

abstract interface class ResponseCache {
  CachedResponse? read(String key);

  void write(String key, CachedResponse response);

  void clear();
}

/// Keeps answers in memory for the life of the app.
class MemoryResponseCache implements ResponseCache {
  final Map<String, CachedResponse> _entries = {};

  @override
  CachedResponse? read(String key) => _entries[key];

  @override
  void write(String key, CachedResponse response) => _entries[key] = response;

  @override
  void clear() => _entries.clear();
}
