using System;
using UnityEngine;

public sealed partial class RJ45ARSession
{
    private bool changingCheckpoint;
    public RJ45CheckpointData CaptureCheckpoint()
    {
        if(!initialized||changingCheckpoint||workstation==null||board==null||preparation==null||trimming==null||insertion==null||crimping==null||tester==null)return null;
        var stage=preparation.Ready?trimming.CheckpointStage:RJ45SavedStage.Prepare;
        if(insertion.IsInserted)stage=RJ45SavedStage.Inserted;
        if(crimping.IsCrimped)stage=RJ45SavedStage.Crimped;
        if(tester.IsPassed)stage=RJ45SavedStage.TestPassed;
        var data=new RJ45CheckpointData{standard=(int)standard,stage=stage,slots=board.CaptureWireSlots()};
        if(!RJ45CheckpointRules.IsValid(data))throw new InvalidOperationException("RJ45 checkpoint state is inconsistent.");
        return data;
    }
    public void ClearCheckpointWorkspace(bool beginPlacement)
    {
        changingCheckpoint=true;
        try
        {
            placement.CancelPlacement();
            if(workstation!=null){workstation.SetActive(false);Destroy(workstation);}
            workstation=null;board=null;preparation=null;trimming=null;insertion=null;crimping=null;tester=null;
            standard=RJ45WiringStandard.T568B;placing=true;waitForRelease=true;
        }
        finally{changingCheckpoint=false;}
        if(beginPlacement)Reposition();
    }
    public void RestoreCheckpoint(RJ45CheckpointData data,Pose pose)
    {
        if(!initialized||!RJ45CheckpointRules.IsValid(data))throw new InvalidOperationException("Invalid RJ45 checkpoint or missing session UI.");
        if(Vector3.Dot(pose.rotation*Vector3.up,Vector3.up)<.95f)throw new InvalidOperationException("Choose a horizontal surface.");
        ClearCheckpointWorkspace(false);changingCheckpoint=true;
        try
        {
            standard=(RJ45WiringStandard)data.standard;
            placement.RestoreRJ45CheckpointPlacement(this,pose);
            if(board==null||preparation==null||trimming==null||insertion==null||crimping==null||tester==null)
                throw new InvalidOperationException("The workstation is missing one or more RJ45 steps.");
            preparation.RestoreCheckpoint(data.stage!=RJ45SavedStage.Prepare);
            board.RestoreWireSlots((RJ45WiringStandard)data.standard,data.slots);
            if(data.stage>=RJ45SavedStage.Arrange)trimming.RestoreCheckpoint(data.stage);
            if(data.stage>=RJ45SavedStage.Inserted)insertion.RestoreInsertedCheckpoint();
            if(data.stage>=RJ45SavedStage.Crimped)crimping.RestoreCrimpedCheckpoint();
            if(data.stage==RJ45SavedStage.TestPassed)tester.RestorePassedCheckpoint();
            preparation.RefreshInstructions();trimming.RefreshInstructions();insertion.RefreshInstructions();crimping.RefreshInstructions();tester.RefreshInstructions();
            waitForRelease=true;placedFrame=Time.frameCount;
        }
        catch {ClearCheckpointWorkspace(false);throw;}
        finally{changingCheckpoint=false;}
    }
}
