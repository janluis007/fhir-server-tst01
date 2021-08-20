﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using MediatR;
using Microsoft.Health.Core.Features.Security.Authorization;
using Microsoft.Health.Fhir.Core.Exceptions;
using Microsoft.Health.Fhir.Core.Features.Security;
using Microsoft.Health.Fhir.Core.Messages.Import;
using Microsoft.Health.TaskManagement;
using Newtonsoft.Json;

namespace Microsoft.Health.Fhir.Core.Features.Operations.Import
{
    public class GetImportRequestHandler : IRequestHandler<GetImportRequest, GetImportResponse>
    {
        private readonly ITaskManager _taskManager;
        private readonly IAuthorizationService<DataActions> _authorizationService;

        public GetImportRequestHandler(ITaskManager taskManager, IAuthorizationService<DataActions> authorizationService)
        {
            EnsureArg.IsNotNull(taskManager, nameof(taskManager));
            EnsureArg.IsNotNull(authorizationService, nameof(authorizationService));

            _taskManager = taskManager;
            _authorizationService = authorizationService;
        }

        public async Task<GetImportResponse> Handle(GetImportRequest request, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(request, nameof(request));

            if (await _authorizationService.CheckAccess(DataActions.Import, cancellationToken) != DataActions.Import)
            {
                throw new UnauthorizedFhirActionException();
            }

            TaskInfo taskInfo = await _taskManager.GetTaskAsync(request.TaskId, cancellationToken);

            if (taskInfo == null)
            {
                throw new ResourceNotFoundException(string.Format(Resources.ImportTaskNotFound, request.TaskId));
            }

            if (taskInfo.Status != TaskManagement.TaskStatus.Completed)
            {
                if (taskInfo.IsCanceled)
                {
                    throw new OperationFailedException(Resources.UserRequestedCancellation, HttpStatusCode.BadRequest);
                }

                ImportOrchestratorTaskInputData inputData = JsonConvert.DeserializeObject<ImportOrchestratorTaskInputData>(taskInfo.InputData);
                ImportTaskResult intermediateResult = ExtractImportState(taskInfo.Context);
                intermediateResult.TransactionTime = inputData.TaskCreateTime;
                return new GetImportResponse(HttpStatusCode.Accepted, intermediateResult);
            }
            else
            {
                TaskResultData resultData = JsonConvert.DeserializeObject<TaskResultData>(taskInfo.Result);
                if (resultData.Result == TaskResult.Success)
                {
                    ImportTaskResult result = JsonConvert.DeserializeObject<ImportTaskResult>(resultData.ResultData);
                    return new GetImportResponse(HttpStatusCode.OK, result);
                }
                else if (resultData.Result == TaskResult.Fail)
                {
                    ImportTaskErrorResult errorResult = JsonConvert.DeserializeObject<ImportTaskErrorResult>(resultData.ResultData);

                    string failureReason = errorResult.ErrorMessage;
                    HttpStatusCode failureStatusCode = errorResult.HttpStatusCode;

                    throw new OperationFailedException(
                        string.Format(Resources.OperationFailed, OperationsConstants.Import, failureReason), failureStatusCode);
                }
                else
                {
                    throw new OperationFailedException(Resources.UserRequestedCancellation, HttpStatusCode.BadRequest);
                }
            }
        }

        private static ImportTaskResult ExtractImportState(string contextInString)
        {
            if (string.IsNullOrEmpty(contextInString))
            {
                return new ImportTaskResult();
            }

            ImportOrchestratorTaskContext context = JsonConvert.DeserializeObject<ImportOrchestratorTaskContext>(contextInString);
            List<ImportOperationOutcome> completedOperationOutcome = new List<ImportOperationOutcome>();
            List<ImportFailedOperationOutcome> failedOperationOutcome = new List<ImportFailedOperationOutcome>();

            foreach ((Uri resourceUri, TaskInfo taskInfo) in context.DataProcessingTasks)
            {
                if (taskInfo.Status == TaskManagement.TaskStatus.Completed)
                {
                    TaskResultData taskResultData = JsonConvert.DeserializeObject<TaskResultData>(taskInfo.Result);
                    if (taskResultData.Result == TaskResult.Success)
                    {
                        ImportProcessingTaskResult processingTaskResult = JsonConvert.DeserializeObject<ImportProcessingTaskResult>(taskResultData.ResultData);
                        completedOperationOutcome.Add(
                            new ImportOperationOutcome()
                            {
                                Type = processingTaskResult.ResourceType,
                                Count = processingTaskResult.SucceedCount,
                                InputUrl = resourceUri,
                            });

                        if (processingTaskResult.FailedCount > 0)
                        {
                            failedOperationOutcome.Add(
                                new ImportFailedOperationOutcome()
                                {
                                    Type = processingTaskResult.ResourceType,
                                    Count = processingTaskResult.FailedCount,
                                    InputUrl = resourceUri,
                                    Url = processingTaskResult.ErrorLogLocation,
                                });
                        }
                    }
                }
            }

            return new ImportTaskResult()
            {
                Output = completedOperationOutcome,
                Error = failedOperationOutcome,
            };
        }
    }
}
