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

@awss3
Scenario: Create and save vault to S3
	Given create a new vault