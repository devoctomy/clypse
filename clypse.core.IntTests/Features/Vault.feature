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

@awss3 @save @delete
Scenario: Create and save vault to S3 then delete the vault
	Given create a new vault
	And key derived from password foobar123
	When vault is saved
	Then vault deleted

#@awss3 @save @addsecret @getsecret @delete
#Scenario: Create and save vault to S3, add a secret, get it back, then delete the vault
#	Given create a new vault
#	And key derived from password foobar123
#	When vault is saved
#	Then vault deleted