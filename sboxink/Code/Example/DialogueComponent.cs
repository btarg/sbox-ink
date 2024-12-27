using Sandbox;
using System.Collections.Generic;
using Ink.Runtime;

public sealed class DialogueComponent : Component
{
    [Description( "File path to the compiled '.ink.json' file, relative to mounted root" )]
    [Property] public string DialogueJsonPath { get; set; } = "ink/test.ink.json";

    Story _inkStory;
    public string CurrentText => _inkStory.currentText;
    public List<Choice> CurrentChoices => _inkStory.currentChoices;
    public List<string> CurrentTags => _inkStory.currentTags;

    public bool IsPlayingDialogue = false;

    protected override void OnAwake()
    {
        StartDialogue();
    }

    public void StartDialogue()
    {
        var data = FileSystem.Mounted.ReadAllText( DialogueJsonPath );
        _inkStory = new Story( data );
        _inkStory.onMakeChoice += ( choice ) => Log.Info( $"Choice made: {choice.text}" );

        _inkStory.onChoosePathString += ( path, obj ) => Log.Info( $"Path chosen: {path}" );
        IsPlayingDialogue = true;
        Continue();
    }
    public void EndDialogue()
    {
        IsPlayingDialogue = false;
        Log.Info( "Dialogue ended" );
    }

    public void Continue()
    {
        if ( _inkStory.canContinue )
        {
            _inkStory.Continue();
        }
        else
        {
            Log.Info( "Cannot continue" );
        }
    }

    public void MakeChoice( int index )
    {
        if ( !_inkStory ) return;
        if ( _inkStory.currentChoices.Count > index )
        {
            _inkStory.ChooseChoiceIndex( index );
            Continue();
        }
    }

    protected override void OnUpdate()
    {
        if ( !_inkStory ) return;

        if ( _inkStory.currentChoices.Count == 0 )
        {
            if ( Input.Pressed( "jump" ) )
            {
                Continue();
            }
            return;
        }

    }

}
