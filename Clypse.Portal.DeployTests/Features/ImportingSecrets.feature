Feature: Importing secrets
    A vault made for the run takes the 200 logins of the medium KeePass file, syncs them, keeps them through locking and
    unlocking, and is deleted again, so the test user is left with only the test vault.

Background:
    Given the sign-in page is open
    And I have signed in as the test user
    And I have opened the vaults page

Scenario: Imported secrets are kept through locking, and their vault is deleted
    When I create a vault named "Deploy import test" with the passphrase "Deploy-import-vault-passphrase-1"
    And I import the KeePass file "medium-import-keepass-1.x.csv"
    Then 200 secrets are imported
    And the vault holds 200 secrets
    And sync is complete
    When I lock the vault
    Then the vault is listed as locked
    When I unlock the vault with the passphrase "Deploy-import-vault-passphrase-1"
    Then the vault holds 200 secrets
    When I delete the vault named "Deploy import test"
    Then the vaults page is shown
    And the vault is listed as being deleted
    And the vault is deleted
    And sync is complete
