
# Reservations Feature

### Requirements
* Handles a request to reserve a vacation property for a given date.
* Interacts with a 3rd party vacation rental property reservation API.
* Saves a Reservation record to database only if reservation API returns confirmation.

### Logic
1) Gets existing reservations from database
2) Creates reservation in memory if passes these validation rules:
   * Reservation must be made at least 7 days in advance
   * An existing reserveration for this property + date does not already exist
3) Tries to obtain a token to 3rd party rental API
4) Tries to reserve property via 3rd party rental API
5) Saves reservation record if success
6) Returns rental API confirmation to calling code (UI)
