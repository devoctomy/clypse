window.S3Client = {
    
    putObject: async function(request) {
        try {
            // Configure AWS credentials
            AWS.config.update({
                accessKeyId: request.AccessKeyId,
                secretAccessKey: request.SecretAccessKey,
                sessionToken: request.SessionToken,
                region: request.Region
            });

            const s3 = new AWS.S3();
            
            // Debug the request body
            if (!request.Body) {
                return {
                    Success: false,
                    ErrorMessage: 'Request body is null or undefined'
                };
            }
            
            // Convert byte array to Uint8Array
            const buffer = new Uint8Array(request.Body);
            
            const params = {
                Bucket: request.Bucket,
                Key: request.Key,
                Body: buffer,
                ContentType: request.ContentType || 'application/octet-stream'
            };

            const result = await s3.putObject(params).promise();
            
            return {
                Success: true,
                Data: {
                    ETag: result.ETag,
                    VersionId: result.VersionId
                }
            };
        } catch (error) {
            return {
                Success: false,
                ErrorMessage: error.message || error.toString()
            };
        }
    },

    getObject: async function(request) {
        try {
            AWS.config.update({
                accessKeyId: request.AccessKeyId,
                secretAccessKey: request.SecretAccessKey,
                sessionToken: request.SessionToken,
                region: request.Region
            });

            const s3 = new AWS.S3();
            
            const params = {
                Bucket: request.Bucket,
                Key: request.Key
            };

            const result = await s3.getObject(params).promise();
            
            // Convert buffer to base64 for transfer to .NET
            const base64Body = btoa(String.fromCharCode(...new Uint8Array(result.Body)));
            
            return {
                Success: true,
                Data: {
                    Body: base64Body,
                    ContentLength: result.ContentLength,
                    ContentType: result.ContentType,
                    ETag: result.ETag,
                    LastModified: result.LastModified.toISOString()
                }
            };
        } catch (error) {
            return {
                Success: false,
                ErrorMessage: error.message || error.toString()
            };
        }
    },

    getObjectMetadata: async function(request) {
        try {
            AWS.config.update({
                accessKeyId: request.AccessKeyId,
                secretAccessKey: request.SecretAccessKey,
                sessionToken: request.SessionToken,
                region: request.Region
            });

            const s3 = new AWS.S3();
            
            const params = {
                Bucket: request.Bucket,
                Key: request.Key
            };

            const result = await s3.headObject(params).promise();
            
            return {
                Success: true,
                Data: {
                    ContentLength: result.ContentLength,
                    ContentType: result.ContentType,
                    ETag: result.ETag,
                    LastModified: result.LastModified.toISOString()
                }
            };
        } catch (error) {
            return {
                Success: false,
                ErrorMessage: error.message || error.toString()
            };
        }
    },

    deleteObject: async function(request) {
        try {
            AWS.config.update({
                accessKeyId: request.AccessKeyId,
                secretAccessKey: request.SecretAccessKey,
                sessionToken: request.SessionToken,
                region: request.Region
            });

            const s3 = new AWS.S3();
            
            const params = {
                Bucket: request.Bucket,
                Key: request.Key
            };

            const result = await s3.deleteObject(params).promise();
            
            return {
                Success: true,
                Data: {
                    DeleteMarker: result.DeleteMarker || false,
                    VersionId: result.VersionId
                }
            };
        } catch (error) {
            return {
                Success: false,
                ErrorMessage: error.message || error.toString()
            };
        }
    },

    listObjectsV2: async function(request) {
        try {
            AWS.config.update({
                accessKeyId: request.AccessKeyId,
                secretAccessKey: request.SecretAccessKey,
                sessionToken: request.SessionToken,
                region: request.Region
            });

            const s3 = new AWS.S3();
            
            const params = {
                Bucket: request.Bucket,
                Prefix: request.Prefix,
                MaxKeys: request.MaxKeys,
                ContinuationToken: request.ContinuationToken
            };

            const result = await s3.listObjectsV2(params).promise();
            
            return {
                Success: true,
                Data: {
                    IsTruncated: result.IsTruncated || false,
                    NextContinuationToken: result.NextContinuationToken,
                    Contents: result.Contents || []
                }
            };
        } catch (error) {
            return {
                Success: false,
                ErrorMessage: error.message || error.toString()
            };
        }
    }
};
