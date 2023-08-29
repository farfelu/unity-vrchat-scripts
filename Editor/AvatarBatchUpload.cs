using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using VRC.SDKBase.Editor;
using VRC.SDKBase.Editor.Api;
using System.Linq;
using VRC.SDK3.Avatars.Components;
using System;
using VRC.SDK3A.Editor;
using VRC.Core;

public class AvatarBatchUpload : MonoBehaviour
{
    [MenuItem("Tools/Batch upload all selected avatars")]
    static async Task UploadSelected()
    {
        if (Selection.gameObjects.Length < 1)
        {
            return;
        }

        // check if we have valid avatars
        // who have already been uploaded at least once
        var objects = Selection.gameObjects;
        var avatarObjects = objects.Where(x =>
        {
            var hasDescriptor = x.GetComponent<VRCAvatarDescriptor>() != null;
            var pipelineComponent = x.GetComponent<PipelineManager>();
            var hasPipelineId = pipelineComponent != null && !string.IsNullOrWhiteSpace(pipelineComponent.blueprintId);

            return hasDescriptor && hasPipelineId;
        }).ToArray();

        if (avatarObjects.Length == 0)
        {
            EditorUtility.DisplayDialog("No avatars selected", "No valid avatars selected.\nIf the avatar hasn't been uploaded yet, upload it manually first", "Ok");
            return;
        }

        // sort them by hierarchy order
        Array.Sort(avatarObjects, (a, b) =>
        {
            if (a == b) return 0;
            if (a == null) return -1;
            if (b == null) return 1;
            return (a.transform.GetSiblingIndex() > b.transform.GetSiblingIndex()) ? 1 : -1;
        });

        // get builder
        if (!VRCSdkControlPanel.TryGetBuilder<IVRCSdkAvatarBuilderApi>(out var builder))
        {
            EditorUtility.DisplayDialog("Error getting builder", "Cannot get builder, check if SDK window is open", "Ok");
            return;
        }

        // progress stuff
        // half the progress for build
        // other half for upload
        //
        // because unity sucks and displayprogressbar can only be called in the main thread,
        // put everything into variables and then call this from the main thread
        var progressText = string.Empty;
        var progress = 0.0f;
        var progressAvatar = 0;
        var progressAvatarName = string.Empty;

        // how much of the progress bar we get per avatar
        var progressSection = 1.0f / avatarObjects.Length;

        // build has 6 stages
        var progressStage = 0;

        EventHandler<string> buildProgress = (s, stage) =>
        {
            var progressStart = progressSection * (progressAvatar - 1.0f);
            progress = progressStart + ((progressSection / 2.0f / 6.0f) * progressStage);
            progressText = stage;

            progressStage++;
        };

        // upload sends its own progress float
        EventHandler<(string, float)> uploadProgres = (s, stage) =>
        {
            var progressPart = (progressSection / 2.0f);
            var progressStart = (progressSection * (progressAvatar - 1.0f)) + progressPart;
            progress = progressStart + (progressPart * stage.Item2);
            progressText = stage.Item1;
        };

        // hook sdk
        builder.OnSdkBuildProgress += buildProgress;
        builder.OnSdkUploadProgress += uploadProgres;



        // remember which objects where enabled
        var enabledObjects = avatarObjects.Where(x => x.activeSelf).ToList();

        // disable them all
        foreach (var obj in avatarObjects)
        {
            obj.SetActive(false);
        }

        // loop over them and upload one by one
        foreach (var avatar in avatarObjects)
        {
            // reset progress stuff
            progressStage = 0;
            progressAvatar++;
            progressAvatarName = avatar.name;
            progressText = "Building...";

            avatar.SetActive(true);

            try
            {
                // both pipelinemanager and vrcavatar id must be set or it'll be treated like a new avatar
                var avatarDescriptor = avatar.GetComponent<VRCAvatarDescriptor>();
                var pipelineComponent = avatar.GetComponent<PipelineManager>();
                var buildTask = builder.BuildAndUpload(avatar, new VRCAvatar()
                {
                    ID = pipelineComponent.blueprintId
                });

                // because progress must be called on the main thread
                while (!buildTask.IsCompleted)
                {
                    EditorUtility.DisplayProgressBar("Batch uploading avatars...", string.Format("{0}/{1} - {2} - {3}", progressAvatar, avatarObjects.Length, progressAvatarName, progressText), progress);
                    await Task.Delay(100);
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
                EditorUtility.DisplayDialog("Error uploading avatar", e.Message + "\n\nBatch upload aborted", "Ok");
                break;
            }

            avatar.SetActive(false);
        }

        EditorUtility.ClearProgressBar();

        //unhook sdk
        builder.OnSdkBuildProgress -= buildProgress;
        builder.OnSdkUploadProgress -= uploadProgres;


        // set them back to their previous state
        foreach (var obj in avatarObjects)
        {
            obj.SetActive(enabledObjects.Contains(obj));
        }
    }
}
