global.AWS = {
    config: {
        update: jest.fn()
    },
    S3: jest.fn()
};

global.btoa = (str) => Buffer.from(str, 'binary').toString('base64');

require('../src/s3-client.js');

describe('S3Client.putObject', () => {
    let mockPutObject;
    let mockS3Instance;

    beforeEach(() => {
        mockPutObject = jest.fn();
        mockS3Instance = {
            putObject: mockPutObject
        };
        global.AWS.S3 = jest.fn(() => mockS3Instance);
        global.AWS.config.update = jest.fn();
    });

    afterEach(() => {
        jest.clearAllMocks();
    });

    test('GivenValidRequest_WhenPutObject_ThenReturnsSuccessWithETagAndVersionId', async () => {
        // Arrange
        const request = {
            accessKeyId: 'test-key',
            secretAccessKey: 'test-secret',
            sessionToken: 'test-token',
            region: 'us-east-1',
            bucket: 'test-bucket',
            key: 'test-key',
            body: 'test-body',
            contentType: 'text/plain'
        };
        const expectedResult = {
            ETag: 'test-etag',
            VersionId: 'test-version'
        };
        mockPutObject.mockReturnValue({
            promise: () => Promise.resolve(expectedResult)
        });

        // Act
        const result = await window.S3Client.putObject(request);

        // Assert
        expect(result.Success).toBe(true);
        expect(result.Data.ETag).toBe('test-etag');
        expect(result.Data.VersionId).toBe('test-version');
        expect(global.AWS.config.update).toHaveBeenCalledWith({
            accessKeyId: 'test-key',
            secretAccessKey: 'test-secret',
            sessionToken: 'test-token',
            region: 'us-east-1'
        });
        expect(mockPutObject).toHaveBeenCalledWith({
            Bucket: 'test-bucket',
            Key: 'test-key',
            Body: 'test-body',
            ContentType: 'text/plain'
        });
    });

    test('GivenNullBody_WhenPutObject_ThenReturnsFailureWithErrorMessage', async () => {
        // Arrange
        const request = {
            accessKeyId: 'test-key',
            secretAccessKey: 'test-secret',
            sessionToken: 'test-token',
            region: 'us-east-1',
            bucket: 'test-bucket',
            key: 'test-key',
            body: null,
            contentType: 'text/plain'
        };

        // Act
        const result = await window.S3Client.putObject(request);

        // Assert
        expect(result.Success).toBe(false);
        expect(result.ErrorMessage).toBe('Request body is null or undefined');
        expect(mockPutObject).not.toHaveBeenCalled();
    });

    test('GivenUndefinedBody_WhenPutObject_ThenReturnsFailureWithErrorMessage', async () => {
        // Arrange
        const request = {
            accessKeyId: 'test-key',
            secretAccessKey: 'test-secret',
            sessionToken: 'test-token',
            region: 'us-east-1',
            bucket: 'test-bucket',
            key: 'test-key',
            contentType: 'text/plain'
        };

        // Act
        const result = await window.S3Client.putObject(request);

        // Assert
        expect(result.Success).toBe(false);
        expect(result.ErrorMessage).toBe('Request body is null or undefined');
        expect(mockPutObject).not.toHaveBeenCalled();
    });

    test('GivenS3Error_WhenPutObject_ThenReturnsFailureWithErrorMessage', async () => {
        // Arrange
        const request = {
            accessKeyId: 'test-key',
            secretAccessKey: 'test-secret',
            sessionToken: 'test-token',
            region: 'us-east-1',
            bucket: 'test-bucket',
            key: 'test-key',
            body: 'test-body',
            contentType: 'text/plain'
        };
        const error = new Error('S3 upload failed');
        mockPutObject.mockReturnValue({
            promise: () => Promise.reject(error)
        });

        // Act
        const result = await window.S3Client.putObject(request);

        // Assert
        expect(result.Success).toBe(false);
        expect(result.ErrorMessage).toBe('S3 upload failed');
    });
});

describe('S3Client.getObject', () => {
    let mockGetObject;
    let mockS3Instance;

    beforeEach(() => {
        mockGetObject = jest.fn();
        mockS3Instance = {
            getObject: mockGetObject
        };
        global.AWS.S3 = jest.fn(() => mockS3Instance);
        global.AWS.config.update = jest.fn();
    });

    afterEach(() => {
        jest.clearAllMocks();
    });

    test('GivenValidRequest_WhenGetObject_ThenReturnsSuccessWithBase64BodyAndMetadata', async () => {
        // Arrange
        const request = {
            accessKeyId: 'test-key',
            secretAccessKey: 'test-secret',
            sessionToken: 'test-token',
            region: 'us-east-1',
            bucket: 'test-bucket',
            key: 'test-key'
        };
        const bodyContent = new Uint8Array([72, 101, 108, 108, 111]);
        const expectedResult = {
            Body: bodyContent,
            ContentLength: 5,
            ContentType: 'text/plain',
            ETag: 'test-etag',
            LastModified: new Date('2026-01-01T00:00:00Z')
        };
        mockGetObject.mockReturnValue({
            promise: () => Promise.resolve(expectedResult)
        });

        // Act
        const result = await window.S3Client.getObject(request);

        // Assert
        expect(result.Success).toBe(true);
        expect(result.Data.Body).toBeDefined();
        expect(result.Data.ContentLength).toBe(5);
        expect(result.Data.ContentType).toBe('text/plain');
        expect(result.Data.ETag).toBe('test-etag');
        expect(result.Data.LastModified).toBe('2026-01-01T00:00:00.000Z');
        expect(global.AWS.config.update).toHaveBeenCalledWith({
            accessKeyId: 'test-key',
            secretAccessKey: 'test-secret',
            sessionToken: 'test-token',
            region: 'us-east-1'
        });
        expect(mockGetObject).toHaveBeenCalledWith({
            Bucket: 'test-bucket',
            Key: 'test-key'
        });
    });

    test('GivenS3Error_WhenGetObject_ThenReturnsFailureWithErrorMessage', async () => {
        // Arrange
        const request = {
            accessKeyId: 'test-key',
            secretAccessKey: 'test-secret',
            sessionToken: 'test-token',
            region: 'us-east-1',
            bucket: 'test-bucket',
            key: 'test-key'
        };
        const error = new Error('Object not found');
        mockGetObject.mockReturnValue({
            promise: () => Promise.reject(error)
        });

        // Act
        const result = await window.S3Client.getObject(request);

        // Assert
        expect(result.Success).toBe(false);
        expect(result.ErrorMessage).toBe('Object not found');
    });
});

describe('S3Client.getObjectMetadata', () => {
    let mockHeadObject;
    let mockS3Instance;

    beforeEach(() => {
        mockHeadObject = jest.fn();
        mockS3Instance = {
            headObject: mockHeadObject
        };
        global.AWS.S3 = jest.fn(() => mockS3Instance);
        global.AWS.config.update = jest.fn();
    });

    afterEach(() => {
        jest.clearAllMocks();
    });

    test('GivenValidRequest_WhenGetObjectMetadata_ThenReturnsSuccessWithMetadata', async () => {
        // Arrange
        const request = {
            accessKeyId: 'test-key',
            secretAccessKey: 'test-secret',
            sessionToken: 'test-token',
            region: 'us-east-1',
            bucket: 'test-bucket',
            key: 'test-key'
        };
        const expectedResult = {
            ContentLength: 1024,
            ContentType: 'application/json',
            ETag: 'test-etag',
            LastModified: new Date('2026-02-15T10:30:00Z')
        };
        mockHeadObject.mockReturnValue({
            promise: () => Promise.resolve(expectedResult)
        });

        // Act
        const result = await window.S3Client.getObjectMetadata(request);

        // Assert
        expect(result.Success).toBe(true);
        expect(result.Data.ContentLength).toBe(1024);
        expect(result.Data.ContentType).toBe('application/json');
        expect(result.Data.ETag).toBe('test-etag');
        expect(result.Data.LastModified).toBe('2026-02-15T10:30:00.000Z');
        expect(global.AWS.config.update).toHaveBeenCalledWith({
            accessKeyId: 'test-key',
            secretAccessKey: 'test-secret',
            sessionToken: 'test-token',
            region: 'us-east-1'
        });
        expect(mockHeadObject).toHaveBeenCalledWith({
            Bucket: 'test-bucket',
            Key: 'test-key'
        });
    });

    test('GivenS3Error_WhenGetObjectMetadata_ThenReturnsFailureWithErrorMessage', async () => {
        // Arrange
        const request = {
            accessKeyId: 'test-key',
            secretAccessKey: 'test-secret',
            sessionToken: 'test-token',
            region: 'us-east-1',
            bucket: 'test-bucket',
            key: 'test-key'
        };
        const error = new Error('Metadata not found');
        mockHeadObject.mockReturnValue({
            promise: () => Promise.reject(error)
        });

        // Act
        const result = await window.S3Client.getObjectMetadata(request);

        // Assert
        expect(result.Success).toBe(false);
        expect(result.ErrorMessage).toBe('Metadata not found');
    });
});

describe('S3Client.deleteObject', () => {
    let mockDeleteObject;
    let mockS3Instance;

    beforeEach(() => {
        mockDeleteObject = jest.fn();
        mockS3Instance = {
            deleteObject: mockDeleteObject
        };
        global.AWS.S3 = jest.fn(() => mockS3Instance);
        global.AWS.config.update = jest.fn();
    });

    afterEach(() => {
        jest.clearAllMocks();
    });

    test('GivenValidRequest_WhenDeleteObject_ThenReturnsSuccessWithDeleteMarkerAndVersionId', async () => {
        // Arrange
        const request = {
            accessKeyId: 'test-key',
            secretAccessKey: 'test-secret',
            sessionToken: 'test-token',
            region: 'us-east-1',
            bucket: 'test-bucket',
            key: 'test-key'
        };
        const expectedResult = {
            DeleteMarker: true,
            VersionId: 'test-version'
        };
        mockDeleteObject.mockReturnValue({
            promise: () => Promise.resolve(expectedResult)
        });

        // Act
        const result = await window.S3Client.deleteObject(request);

        // Assert
        expect(result.Success).toBe(true);
        expect(result.Data.DeleteMarker).toBe(true);
        expect(result.Data.VersionId).toBe('test-version');
        expect(global.AWS.config.update).toHaveBeenCalledWith({
            accessKeyId: 'test-key',
            secretAccessKey: 'test-secret',
            sessionToken: 'test-token',
            region: 'us-east-1'
        });
        expect(mockDeleteObject).toHaveBeenCalledWith({
            Bucket: 'test-bucket',
            Key: 'test-key'
        });
    });

    test('GivenNoDeleteMarker_WhenDeleteObject_ThenReturnsSuccessWithDefaultFalseDeleteMarker', async () => {
        // Arrange
        const request = {
            accessKeyId: 'test-key',
            secretAccessKey: 'test-secret',
            sessionToken: 'test-token',
            region: 'us-east-1',
            bucket: 'test-bucket',
            key: 'test-key'
        };
        const expectedResult = {
            VersionId: 'test-version'
        };
        mockDeleteObject.mockReturnValue({
            promise: () => Promise.resolve(expectedResult)
        });

        // Act
        const result = await window.S3Client.deleteObject(request);

        // Assert
        expect(result.Success).toBe(true);
        expect(result.Data.DeleteMarker).toBe(false);
        expect(result.Data.VersionId).toBe('test-version');
    });

    test('GivenS3Error_WhenDeleteObject_ThenReturnsFailureWithErrorMessage', async () => {
        // Arrange
        const request = {
            accessKeyId: 'test-key',
            secretAccessKey: 'test-secret',
            sessionToken: 'test-token',
            region: 'us-east-1',
            bucket: 'test-bucket',
            key: 'test-key'
        };
        const error = new Error('Delete operation failed');
        mockDeleteObject.mockReturnValue({
            promise: () => Promise.reject(error)
        });

        // Act
        const result = await window.S3Client.deleteObject(request);

        // Assert
        expect(result.Success).toBe(false);
        expect(result.ErrorMessage).toBe('Delete operation failed');
    });
});

describe('S3Client.listObjectsV2', () => {
    let mockListObjectsV2;
    let mockS3Instance;

    beforeEach(() => {
        mockListObjectsV2 = jest.fn();
        mockS3Instance = {
            listObjectsV2: mockListObjectsV2
        };
        global.AWS.S3 = jest.fn(() => mockS3Instance);
        global.AWS.config.update = jest.fn();
    });

    afterEach(() => {
        jest.clearAllMocks();
    });

    test('GivenValidRequest_WhenListObjectsV2_ThenReturnsSuccessWithContentsAndCommonPrefixes', async () => {
        // Arrange
        const request = {
            accessKeyId: 'test-key',
            secretAccessKey: 'test-secret',
            sessionToken: 'test-token',
            region: 'us-east-1',
            bucket: 'test-bucket',
            prefix: 'test-prefix/',
            maxKeys: 100,
            continuationToken: null,
            delimiter: '/'
        };
        const expectedResult = {
            IsTruncated: false,
            NextContinuationToken: null,
            Contents: [
                { Key: 'test-prefix/file1.txt', Size: 1024 },
                { Key: 'test-prefix/file2.txt', Size: 2048 }
            ],
            CommonPrefixes: [
                { Prefix: 'test-prefix/folder1/' },
                { Prefix: 'test-prefix/folder2/' }
            ]
        };
        mockListObjectsV2.mockReturnValue({
            promise: () => Promise.resolve(expectedResult)
        });

        // Act
        const result = await window.S3Client.listObjectsV2(request);

        // Assert
        expect(result.Success).toBe(true);
        expect(result.Data.IsTruncated).toBe(false);
        expect(result.Data.NextContinuationToken).toBe(null);
        expect(result.Data.Contents).toHaveLength(2);
        expect(result.Data.CommonPrefixes).toEqual(['test-prefix/folder1/', 'test-prefix/folder2/']);
        expect(global.AWS.config.update).toHaveBeenCalledWith({
            accessKeyId: 'test-key',
            secretAccessKey: 'test-secret',
            sessionToken: 'test-token',
            region: 'us-east-1'
        });
        expect(mockListObjectsV2).toHaveBeenCalledWith({
            Bucket: 'test-bucket',
            Prefix: 'test-prefix/',
            MaxKeys: 100,
            ContinuationToken: null,
            Delimiter: '/'
        });
    });

    test('GivenTruncatedResult_WhenListObjectsV2_ThenReturnsSuccessWithContinuationToken', async () => {
        // Arrange
        const request = {
            accessKeyId: 'test-key',
            secretAccessKey: 'test-secret',
            sessionToken: 'test-token',
            region: 'us-east-1',
            bucket: 'test-bucket',
            prefix: 'test-prefix/',
            maxKeys: 10,
            continuationToken: null,
            delimiter: '/'
        };
        const expectedResult = {
            IsTruncated: true,
            NextContinuationToken: 'next-token-123',
            Contents: [{ Key: 'test-prefix/file1.txt', Size: 1024 }],
            CommonPrefixes: []
        };
        mockListObjectsV2.mockReturnValue({
            promise: () => Promise.resolve(expectedResult)
        });

        // Act
        const result = await window.S3Client.listObjectsV2(request);

        // Assert
        expect(result.Success).toBe(true);
        expect(result.Data.IsTruncated).toBe(true);
        expect(result.Data.NextContinuationToken).toBe('next-token-123');
    });

    test('GivenEmptyBucket_WhenListObjectsV2_ThenReturnsSuccessWithEmptyContentsAndEmptyPrefixes', async () => {
        // Arrange
        const request = {
            accessKeyId: 'test-key',
            secretAccessKey: 'test-secret',
            sessionToken: 'test-token',
            region: 'us-east-1',
            bucket: 'test-bucket',
            prefix: 'test-prefix/',
            maxKeys: 100,
            continuationToken: null,
            delimiter: '/'
        };
        const expectedResult = {
            IsTruncated: false
        };
        mockListObjectsV2.mockReturnValue({
            promise: () => Promise.resolve(expectedResult)
        });

        // Act
        const result = await window.S3Client.listObjectsV2(request);

        // Assert
        expect(result.Success).toBe(true);
        expect(result.Data.IsTruncated).toBe(false);
        expect(result.Data.Contents).toEqual([]);
        expect(result.Data.CommonPrefixes).toEqual([]);
    });

    test('GivenS3Error_WhenListObjectsV2_ThenReturnsFailureWithErrorMessage', async () => {
        // Arrange
        const request = {
            accessKeyId: 'test-key',
            secretAccessKey: 'test-secret',
            sessionToken: 'test-token',
            region: 'us-east-1',
            bucket: 'test-bucket',
            prefix: 'test-prefix/',
            maxKeys: 100,
            continuationToken: null,
            delimiter: '/'
        };
        const error = new Error('List operation failed');
        mockListObjectsV2.mockReturnValue({
            promise: () => Promise.reject(error)
        });

        // Act
        const result = await window.S3Client.listObjectsV2(request);

        // Assert
        expect(result.Success).toBe(false);
        expect(result.ErrorMessage).toBe('List operation failed');
    });
});
