
# Business Process Description

Design a SQL Server application that implements a `Conference Management System` called `ConfMSmini`.

NOTES: 
- You (the AI Agent) **must not** edit this file.
- Human reader (not the AI):
  1. Copy and paste `Follow instructions in current file` into the Agent-Mode chat window.
  2. You'll need to type `proceed` about 15 times.
  3. At the end you should have about "40 files changed", press the `Keep` button.

# Roles

The roles are:

- Attendees - the attendees who attend the conference, the attendees register, buy a ticket
- Speakers - the speakers who attend the conference, they submit sessions that will be given at a conference.  Speakers can submit multiple sessions for the same conference.
- Organizer - manage the overall conference, and can generate a full set of reports, like track ticket sales etc.

# Entities

The entities are:

- Conference - the conference information
- User - the user information for the organizers, attendees and speakers (everyone is a user)
- UserRoleType - the type of role (attendee, speaker, organizer)
- UserRole - the role(s) of the user (attendee, speaker, organizer)
- Ticket - the ticket information for the attendees
- TicketType - the type of ticket (early bird, regular, late)
- Session - the session information for the speakers
- AttendeeSessions - the sessions the attendee wants to attend

# Entity Descriptions

The entity descriptions are:

- Conference - the conference information
  - ConferenceId - the unique identifier for the conference
  - ConferenceName - the name of the conference
  - ConferenceDescription - the description of the conference
  - StartDate - the start date of the conference
  - EndDate - the end date of the conference
  - Location - the location of the conference
  - WebsiteUrl - the website URL for the conference
- User - user information for organizers, attendees and speakers
  - UserId - the unique identifier for the user
  - FirstName - the first name of the user
  - LastName - the last name of the user
  - UserName - the user name of the user
- UserRole - the role(s) of the user (attendee, speaker, organizer)
  - UserId - the unique identifier for the user
  - UserRoleTypeId - the unique identifier for the user role
- Ticket - the ticket information for the attendees
  - TicketId - the unique identifier for the ticket
  - UserId - the unique identifier for the user
  - ConferenceId - the unique identifier for the conference
  - TicketType - the type of ticket (early bird, regular, late)
- TicketType - the type of ticket (early bird, regular, late)
  - TicketTypeId - the unique identifier for the ticket type
  - TicketTypeName - the name of the ticket type
  - TicketTypeDescription - the description of the ticket type
  - TicketTypePrice - the price of the ticket type
- Session - the session information for the speakers
  - SessionId - the unique identifier for the session
  - SessionTitle - the title of the session
  - SessionDescription - the description of the session
  - SpeakerId - the unique identifier for the speaker
  - ConferenceId - the unique identifier for the conference
  - Length - the length of the session (in minutes)
- Schedule - the schedule for sessions at a conference
  - ScheduleId - the unique identifier for the session schedule
  - SessionId - the unique identifier for the session
  - ConferenceId - the unique identifier for the conference
  - StartDateTime - the start date and time of the session
  - EndDateTime - the end date and time of the session
- AttendeeSessions - the sessions the attendee wants to attend
  - AttendeeSessionId - the unique identifier for the attendee session
  - UserId - the unique identifier for the user
  - SessionId - the unique identifier for the session
  - ConferenceId - the unique identifier for the conference

# Business Process Operations

The operations per role are:

- All Roles (these operations are available to all roles)
  - Register as a user
    - Register as user should generate a unique username (if one is not passed in) using the First Name and Last Name, it must be unique.  By default use the first 6 characters of the first name, and the first two characters of the 2nd name.  If the username is not unique, append a number to the end of the username, starting with 1.  For example, if the first name is "John" and the last name is "Doe", the username would be "johndo".  If that username already exists, the next one would be "johndo1", and so on.
  - Get username
    Based on first name and last name return the username.
  - See a list of conferences (that haven't happened yet)
- Organizer
  - Register as a user (implicitly assign the organizer, attendee, speaker role)
  - Add a conference
  - Add a ticket type
  - See ticket sales
    - See a list of tickets sold for a conference
  - Generate Schedule
    - Generates a schedule for a conference, based on the sessions (which includes their length) and the speakers. Ensure a speaker is not scheduled for more than one session at the same time.  Ensure sessions start are 9am, and end by 5pm.  Ensure lunch is 12pm to 1pm.
- Attendees
  - See a list of conferences (that haven't happened yet)
  - Register as a user (implicitly assign the attendee role)
  - Buy a ticket for a conference
  - Build a list of sessions they want to attend for a conference
- Speakers
  - Register as a user (implicitly assign the attendee and speaker role)
  - Submit session proposal for a conference

# User Stories

These are end to end user stories for the system:

- User Story "The Happy Path" (Positive case)
  - Organizer James Organizer `jimorg` registers as a user in the system
  - Organizer `jimorg` creates a conference in June 2025 for 3 days
  - Organizer `jimorg` creates ticket types, early bird, regular and late
  - Attendee John Doe `johndoe` registers as a user
  - Attendee `johndoe` buys an early bird ticket
  - Speaker Jane Doe `janedoe` registers as a user
  - Speaker `janedoe` submits a session proposal 
  - Organizer `jimorg` tracks ticket sales
  - Organizer `jimorg` generates the conference schedule
  - Attendee `johndoe` builds a list of sessions they want to attend
- User Story "Trying to buy a ticket before registering" (Negative case)
  - Attendee John Doe `johndoe` buys an early bird ticket (it should fail)
- User Story "Trying to buy a ticket before a conference is created"  (Negative case)
  - Attendee John Doe `johndoe` registers as a user
  - Attendee John Doe `johndoe` buys an early bird ticket (it should fail)
- User Story "Submit a session proposal for a non existent conference" (Negative case)
  - Speaker Jane Doe `janedoe` registers as a user
  - Speaker `janedoe` submits a session proposal for a non existent conference (it should fail)
- User Story "Attend tries to build a session list of a conference they have not bought a ticket for"
  - Attendee John Doe `johndoe` registers as a user
  - Attended `johndoe` builds a session list for a conference they have not bought a ticket for (it should fail) 
- User Story "Trying to buy a ticket for a conference that has already happened" (Negative case)
  - Attendee John Doe `johndoe` registers as a user
  - Attendee `johndoe` buys an early bird ticket for a conference that has already happened (it should fail)
- User Story "An attendee trying to buy a second ticket for the same conference" (Negative case)
  - Attendee John Doe `johndoe` registers as a user
  - Attendee `johndoe` buys an early bird ticket
  - Attendee `johndoe` buys a second early bird ticket for the same conference (it should fail)
