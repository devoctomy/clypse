Feature: Signing in
    The deployed portal signs the test user in and out with the real user pool.

Background:
    Given the sign-in page is open

Scenario: The test user signs in
    When I sign in as the test user
    Then the vaults page is shown
    And the sign-in form is gone

Scenario: A wrong password is refused
    When I sign in as the test user with a wrong password
    Then a sign-in error is shown
    And the sign-in page is shown

Scenario: A signed-in user signs out
    Given I have signed in as the test user
    When I sign out
    Then the sign-in page is shown
