﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Buzz.Extensions
{
    static class StorageExtensions
    {
        public static async Task CopyContainerByName(this string containerName, CloudBlobClient sourceClient,
            CloudBlobClient targetClient)
        {
            var sourceBlobs = (await sourceClient.GetContainerReference(containerName)
                .ListBlobsSegmentedAsync(new BlobContinuationToken())).Results;
            // Create a policy for reading the blob.
            var targetContainer = await targetClient.CreateCotanier(containerName);
            foreach (var blob in sourceBlobs)
            {
                await new CloudBlockBlob(blob.Uri, sourceClient.Credentials).Copy(targetContainer);
            }
        }
        
        private static async Task<CloudBlobContainer> CreateCotanier(this CloudBlobClient blobClient, string containerName)
        {
            var targetContainer = blobClient.GetContainerReference(containerName);
            await targetContainer.CreateIfNotExistsAsync();
            await targetContainer.SetPermissionsAsync(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });
            return targetContainer;
        }

        private static async Task Copy(this CloudBlockBlob sourceBlob, CloudBlobContainer targetContainer)
        {
            // Get SAS of that policy.
            var sourceBlobToken = sourceBlob.GetSharedAccessSignature(
                new SharedAccessBlobPolicy
                {
                    Permissions = SharedAccessBlobPermissions.Read,
                    SharedAccessStartTime = DateTime.UtcNow.AddMinutes(-15),
                    SharedAccessExpiryTime = DateTime.UtcNow.AddDays(7)
                });
            // Make a full uri with the sas for the blob.
            var targetBlob = targetContainer.GetBlockBlobReference(sourceBlob.Name);
            if (!await targetBlob.ExistsAsync())
            {
                await targetBlob.StartCopyAsync(new Uri($"{sourceBlob.Uri}{sourceBlobToken}"));
                await WaitSuccessCopy(targetContainer, sourceBlob.Name);
            }
        }

        private static async Task WaitSuccessCopy(CloudBlobContainer targetContainer, string name)
        {
            var found = false;
            while (!found)
            {
                if ((await targetContainer.ListBlobsSegmentedAsync(name,
                        new BlobContinuationToken())).Results.FirstOrDefault() is CloudBlob destBlob && destBlob.CopyState.Status == CopyStatus.Success)
                    found = true;
                else
                    Thread.Sleep(1000);
            }
        }
    }
}
