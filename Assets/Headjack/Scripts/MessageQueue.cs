// Copyright (c) Headjack (part of Purple Pill VR B.V.), All rights reserved.

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Headjack;
internal class MessageQueue : MonoBehaviour {
	private List<MessageInfo> queue;
    public TextMesh text;
	private MeshRenderer myMesh, tMesh;
    private bool show;
	void Start(){
        App.PopUp = this;
        myMesh = GetComponent<MeshRenderer> ();
        tMesh = text.GetComponent<MeshRenderer>();
        queue = new List<MessageInfo>();
        show = myMesh.enabled = tMesh.enabled = false;
    }

    void Update()
    {
        if (queue.Count > 0)
        {
            if (!show)
            {
                show = myMesh.enabled = tMesh.enabled = true;
                Tools.FitTextToBounds(text, queue[0].Text, new Vector2(transform.localScale.x-0.05f, transform.localScale.y-0.015f),22,32);
            }
            if (queue[0].TimeInSeconds > -0.5)
            {
                queue[0].TimeInSeconds -= Time.unscaledDeltaTime;
                if (queue[0].TimeInSeconds < 0)
                {
                    myMesh.enabled = tMesh.enabled = false;
                }
            }
            else {
                if (queue[0].onEnd != null)
                {
                    queue[0].onEnd(true,null);
                }
                queue.RemoveAt(0);
                show = false;
            }
        }
        else {
            if (show)
            {
                show = myMesh.enabled = tMesh.enabled = false;
            }
        }
    }

	internal void Add(string Message, float TimeInSeconds, OnEnd onEnd)
	{
		MessageInfo M = new MessageInfo ();
		M.Text = Message;
        M.TimeInSeconds = TimeInSeconds;
        M.onEnd = onEnd;
		queue.Add (M);
	}

	class MessageInfo
	{
		internal string Text;
		internal float TimeInSeconds;
        internal OnEnd onEnd=null;
	}
}


