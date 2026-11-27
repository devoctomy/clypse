Feature: Files in the test vault
    The test vault takes a small image and a small text file. Each is added, viewed or edited, and deleted again, so the
    test vault is left as it was. A file of the same name left by a failed run is deleted first.

Background:
    Given the sign-in page is open
    And I have signed in as the test user
    And I have opened the vaults page

Scenario: An image is added, viewed and deleted
    Given the test user has a vault, or the scenario ends here
    And the first sync has finished
    When I unlock the test vault with the passphrase "Deploy-test-vault-passphrase-1"
    And I open the vault's files
    And I delete any file named "deploy-test-image.png"
    And I add the file "deploy-test-image.png"
    Then the file "deploy-test-image.png" is listed
    And sync is complete
    When I open the file "deploy-test-image.png"
    Then the viewer shows its image
    When I close the viewer
    And I delete the file "deploy-test-image.png"
    Then the file "deploy-test-image.png" is not listed
    And sync is complete

Scenario: A text file is added, edited and deleted
    Given the test user has a vault, or the scenario ends here
    And the first sync has finished
    When I unlock the test vault with the passphrase "Deploy-test-vault-passphrase-1"
    And I open the vault's files
    And I delete any file named "deploy-test-notes.txt"
    And I add the file "deploy-test-notes.txt"
    Then the file "deploy-test-notes.txt" is listed
    And sync is complete
    When I open the file "deploy-test-notes.txt"
    Then the editor shows the text "Deploy test notes."
    When I change the text to "Deploy test notes, edited."
    And I save the text
    And I close the editor
    And I open the file "deploy-test-notes.txt"
    Then the editor shows the text "Deploy test notes, edited."
    When I close the editor
    And I delete the file "deploy-test-notes.txt"
    Then the file "deploy-test-notes.txt" is not listed
    And sync is complete
