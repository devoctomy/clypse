window.S3Client = {
    
    putObject: async function(request) {
        try {
            AWS.config.update({
                accessKeyId: request.accessKeyId,
                secretAccessKey: request.secretAccessKey,
                sessionToken: request.sessionToken,
                region: request.region
            });

            const s3 = new AWS.S3();
            
            if (!request.body) {
                return {
                    Success: false,
                    ErrorMessage: 'Request body is null or undefined'
                };
            }
                  
            const params = {
                Bucket: request.bucket,
                Key: request.key,
                Body: request.body,
                ContentType: request.contentType
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
                accessKeyId: request.accessKeyId,
                secretAccessKey: request.secretAccessKey,
                sessionToken: request.sessionToken,
                region: request.region
            });

            const s3 = new AWS.S3();
            
            const params = {
                Bucket: request.bucket,
                Key: request.key
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
                accessKeyId: request.accessKeyId,
                secretAccessKey: request.secretAccessKey,
                sessionToken: request.sessionToken,
                region: request.region
            });

            const s3 = new AWS.S3();
            
            const params = {
                Bucket: request.bucket,
                Key: request.key
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
                accessKeyId: request.accessKeyId,
                secretAccessKey: request.secretAccessKey,
                sessionToken: request.sessionToken,
                region: request.region
            });

            const s3 = new AWS.S3();
            
            const params = {
                Bucket: request.bucket,
                Key: request.key
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
                accessKeyId: request.accessKeyId,
                secretAccessKey: request.secretAccessKey,
                sessionToken: request.sessionToken,
                region: request.region
            });

            const s3 = new AWS.S3();
            
            const params = {
                Bucket: request.bucket,
                Prefix: request.prefix,
                MaxKeys: request.maxKeys,
                ContinuationToken: request.continuationToken,
                Delimiter: request.delimiter
            };

            const result = await s3.listObjectsV2(params).promise();
            
            return {
                Success: true,
                Data: {
                    IsTruncated: result.IsTruncated || false,
                    NextContinuationToken: result.NextContinuationToken,
                    Contents: result.Contents || [],
                    CommonPrefixes: (result.CommonPrefixes || []).map(cp => cp.Prefix)
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
