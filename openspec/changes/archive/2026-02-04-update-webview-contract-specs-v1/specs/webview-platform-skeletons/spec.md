## ADDED Requirements

### Requirement: Gtk adapter skeleton does not imply Linux embedded baseline support
The platform adapter skeleton requirements SHALL NOT be interpreted as a promise of Baseline Linux Embedded WebView support.
If a Gtk adapter skeleton exists, it SHALL be treated as optional/extended and not as evidence of Baseline Embedded support on Linux.

#### Scenario: Platform skeletons do not claim Linux embedded baseline
- **WHEN** a consumer reads the platform adapter skeleton spec and sees the Gtk adapter project listed
- **THEN** they do not conclude Baseline Embedded WebView support is promised for Linux

