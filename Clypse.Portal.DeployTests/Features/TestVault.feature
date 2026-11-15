Feature: The test vault
    The test user keeps one vault, the test vault, holding one login. The first run creates it; every run after that
    unlocks it and copies the login's user name and password.

Background:
    Given the sign-in page is open
    And I have signed in as the test user
    And I have opened the vaults page

Scenario: The test vault is created on the first run
    Given the test user has no vault yet, or the scenario ends here
    When I create the test vault named "Deploy tests" with the passphrase "Deploy-test-vault-passphrase-1"
    And I add a login named "Deploy test login" with user name "deploy.test.user" and password "Deploy-test-password-1"
    And I lock the vault
    Then the test vault is listed as locked
    And sync is complete

Scenario: The test vault's login is copied
    Given the test user has a vault, or the scenario ends here
    And the first sync has finished
    When I unlock the test vault with the passphrase "Deploy-test-vault-passphrase-1"
    And I search for the secret "Deploy test login"
    Then the secret "Deploy test login" is found
    When I copy its user name
    Then the clipboard holds "deploy.test.user"
    When I copy its password
    Then the clipboard holds "Deploy-test-password-1"

Scenario: A login with a generated password reaches another device
    Given the test user has a vault, or the scenario ends here
    When I unlock the test vault with the passphrase "Deploy-test-vault-passphrase-1"
    And I add a login named "Generated login" with the date and time, user name "deploy.test.user" and a random password from the generator
    And I lock the vault
    Then the test vault is listed as locked
    And sync is complete
    When I sign out
    Then the sign-in page is shown
    When I open the portal on another device
    And I sign in as the test user
    Then the vaults page is shown
    And the first sync has finished
    When I unlock the test vault with the passphrase "Deploy-test-vault-passphrase-1"
    And I search for the generated login
    Then the generated login is found
    And its password is the one the generator made
