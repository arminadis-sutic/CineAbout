class Validators {
  static final _email = RegExp(r'^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$');
  static final _username = RegExp(r'^[A-Za-z0-9.]{3,50}$');
  static final _phone = RegExp(r'^\+?[0-9 ()/-]{6,20}$');

  static String? required(String? v, String field, {int max = 150}) {
    if (v == null || v.trim().isEmpty) return '$field is required.';
    if (v.trim().length > max) return '$field must have at most $max characters.';
    return null;
  }

  static String? email(String? v) {
    if (v == null || v.trim().isEmpty) return 'E-mail is required.';
    if (!_email.hasMatch(v.trim())) return 'Enter a valid e-mail in the format name@domain.com.';
    return null;
  }

  static String? username(String? v) {
    if (v == null || !_username.hasMatch(v.trim())) {
      return 'Username: 3-50 characters, letters A-Z, numbers and dots only.';
    }
    return null;
  }

  static String? password(String? v, {String field = 'Password'}) {
    if (v == null || v.isEmpty) return '$field is required.';
    final ok = v.length >= 8 &&
        v.length <= 100 &&
        RegExp(r'[a-z]').hasMatch(v) &&
        RegExp(r'[A-Z]').hasMatch(v) &&
        RegExp(r'\d').hasMatch(v) &&
        RegExp(r'[^\da-zA-Z]').hasMatch(v);
    return ok
        ? null
        : '$field needs 8-100 characters with an uppercase letter, a lowercase letter, a number and a special character (e.g. Lozinka123!).';
  }

  static String? confirm(String? v, String original) =>
      v == original ? null : 'The passwords do not match.';

  static String? phone(String? v) {
    if (v == null || v.trim().isEmpty) return null;
    return _phone.hasMatch(v.trim()) ? null : 'Enter a phone number like +387 61 123 456 (6-20 digits).';
  }
}
