Feature: Vault sync with AmazonS3

Assures vaults can be created, and synchronised with AmazonS3.

Background:
	Given aws access key loaded from environment variable
	And aws secret access key loaded from environment variable
	And aws bucket name loaded from environment variable
	And crypto service is initialised
	And aws cloud service provider is initialised
	And compression service is initialised
	And vault manager is initialised

@awss3 @vault @secret @crud
Scenario: Create and save vault to S3, perform basic CRUD operations on secrets, then delete the vault
	Given create a new vault
	And key derived from password foobar123
	And web secrets are added
		| Name    | Description           | UserName               | Password  |
		| Secret1 | Some secret thing.    | bob@hoskins.com        | foobar123 |
		| Secret2 | Another secret thing. | bob.hoskins@foobar.com | 123foobar |
	And vault is saved
	When vault is loaded
	And secret Secret1 is loaded and matches added
	And secret Secret2 is loaded and matches added
	Then secret Secret1 is marked for deletion
	And vault is saved
	And vault is loaded
	And secret Secret1 does not exist
	And secret Secret2 is loaded and matches added
	And vault deleted