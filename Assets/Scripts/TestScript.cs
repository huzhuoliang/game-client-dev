using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Serialization;

public class TestScript : MonoBehaviour {
    
    [SerializeField]
    [LabelText("Update Mode")]
    private DirectorUpdateMode updateMode;

    [SerializeField]
    [LabelText("Animator")]
    private Animator animator;

    [SerializeField]
    [LabelText("Animation Clip 1")]
    private AnimationClip animationClip1;

    [SerializeField]
    [LabelText("Animation Clip 1 Weight")]
    [Range(0f, 1f)]
    private float clip1Weight;
    
    [SerializeField]
    [LabelText("Animation Clip 2")]
    private AnimationClip animationClip2;


    private PlayableGraph _playableGraph;

    private AnimationPlayableOutput _animationPlayableOutput;
    
    private AnimationClipPlayable _animationClipPlayable1;
    private AnimationClipPlayable _animationClipPlayable2;

    private AnimationMixerPlayable _animationMixerPlayable;

    private void OnEnable() {
        _playableGraph = PlayableGraph.Create();
        _playableGraph.SetTimeUpdateMode(updateMode);

        // AnimationPlayableUtilities.PlayClip(animator, animationClip, out _playableGraph);
        
        _animationPlayableOutput = AnimationPlayableOutput.Create(_playableGraph, "MyAnime1", animator);
        _animationClipPlayable1 = AnimationClipPlayable.Create(_playableGraph, animationClip1);
        _animationClipPlayable2 = AnimationClipPlayable.Create(_playableGraph, animationClip2);
        _animationMixerPlayable = AnimationMixerPlayable.Create(_playableGraph, 2);
        
        _animationPlayableOutput.SetSourcePlayable(_animationMixerPlayable);

        _playableGraph.Connect(_animationClipPlayable1, 0, _animationMixerPlayable, 0);
        _playableGraph.Connect(_animationClipPlayable2, 0, _animationMixerPlayable, 1);
        
        _animationMixerPlayable.SetInputWeight(0, clip1Weight);
        _animationMixerPlayable.SetInputWeight(1, 1 - clip1Weight);
        
        _playableGraph.Play();
    }

    private void Update() {
        _animationMixerPlayable.SetInputWeight(0, clip1Weight);
        _animationMixerPlayable.SetInputWeight(1, 1 - clip1Weight);
    }

    private void OnDisable() {
        _playableGraph.Stop();
        _playableGraph.Destroy();
    }

    public void Log() {
        
    }
}
