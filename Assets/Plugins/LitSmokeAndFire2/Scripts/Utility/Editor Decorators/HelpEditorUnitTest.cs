using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HelpEditorUnitTest : MonoBehaviour
{

    public bool _bFoldOutMasterr;

    [SerializeField]
    [Help("Herp Derp and Bursdfasdafdddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddd", "_bFoldOutMasterr")]
    protected bool _bUnitTestHelp2;

    [CleanInspectorName("", "_bFoldOutMasterr")]
    public bool _bFoldOutSub;

    [SerializeField]
    [Help("Herp Derp and Bursdfasdafdddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddd")]
    protected bool _bUnitTestHelp;
}
