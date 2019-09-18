MODULE Messaging

    RECORD SD
        string MoveMethod;
        pos RobTarg;
        orient Orientation;
        robjoint JointTarget;
        num TCPSpeed;
        num ReorSpeed;
        pose ToolFrame;
!        num ZoneTCP;
!        num ZoneOrg;
    ENDRECORD
    
    RECORD SE
        string Hello;
    ENDRECORD
 
    PERS tooldata StreamingTool:=[TRUE,[[31.6965,61.8812,133.379],[0.207982,0.10829,0.44888,0.862278]],[1,[0,0,1E-04],[1,0,0,0],0,0,0]];
    VAR speeddata StreamingSpeed:= [ 100, 20, 200, 15 ];
    VAR zonedata StreamingZone:= [False, 0.3, 0.3, 0.3, 0.03, 0.3, 0.03 ];

    VAR bool flag:=FALSE;
    VAR intnum connectionNumber;
    VAR SD MyData;

    PROC main()
        ConfL \Off;
        ConfJ \Off;
        CONNECT connectionNumber WITH Process;
        IRMQMessage MyData,connectionNumber;

        WHILE flag=FALSE DO
            ! Idle wait for connection.
            WaitTime 0.01;
        ENDWHILE

        TPWrite "RMQ message received from server. Acknowledgement sent.";
        IDelete connectionNumber;

        EXIT;
    ENDPROC

    TRAP Process
        VAR rmqmessage msg;
        VAR rmqheader header;
        VAR rmqslot rabclient;
        VAR num userdef;
        VAR string ack:="Message received from GH.";

        RMQGetMessage msg;
    	RMQGetMsgHeader msg \Header:= header\SenderId:=rabclient\UserDef:=userdef;
!    	RMQSendMessage rabclient, ack;
        
        IF header.datatype="SD" THEN    
            RMQGetMsgData msg, MyData;
            
            StreamingTool.tframe:=MyData.ToolFrame;
            StreamingSpeed.v_tcp:=MyData.TCPSpeed;
            StreamingSpeed.v_ori:=MyData.ReorSpeed;
!            StreamingZone.pzone_tcp:=MyData.ZoneTCP;
!            StreamingZone.pzone_ori:=MyData.ZoneOrg;
            
            If MyData.MoveMethod="Linear" THEN
                StorePath;
                MoveL [MyData.RobTarg,MyData.Orientation,[0,0,0,0],[9E9,9E9,9E9,9E9,9E9,9E9]],StreamingSpeed,StreamingZone,StreamingTool;
                RestoPath;               
            ENDIF
            
            If MyData.MoveMethod="Joint" THEN
                StorePath;
                MoveJ [MyData.RobTarg,MyData.Orientation,[0,0,0,0],[9E9,9E9,9E9,9E9,9E9,9E9]],StreamingSpeed,StreamingZone,StreamingTool;
                RestoPath;               
            ENDIF
            
            If MyData.MoveMethod="AbsoluteJoint" THEN
                StorePath;
                MoveAbsJ [MyData.JointTarget, [0, 0, 9E9, 9E9, 9E9, 9E9]], StreamingSpeed, StreamingZone, StreamingTool;
                RestoPath;               
            ENDIF
            
        ENDIF
        IF header.datatype="SE" THEN    
            RMQGetMsgData msg, MyData;
        ENDIF
    ENDTRAP
ENDMODULE
