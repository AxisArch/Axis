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
    
    RECORD ME
        string Text;
    ENDRECORD
 
    PERS tooldata StreamingTool:=[TRUE,[[0,0,0],[1,0,0,0]],[1,[0,0,1E-04],[1,0,0,0],0,0,0]];
    VAR speeddata StreamingSpeed:= [ 100, 20, 200, 15 ];
    !VAR zonedata StreamingZone:= [False, 0.3, 0.3, 0.3, 0.03, 0.3, 0.03 ];
    VAR zonedata StreamingZone:= [False, 150, 150, 10, 1.5, 150, 1.5 ];

    VAR bool flag:=FALSE;
    VAR intnum connectionNumber;
    VAR SD MyData;
    VAR ME Message;

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

        ELSEIF header.datatype="ME" THEN    
            RMQGetMsgData msg, Message;
            TPWrite Message.Text;
		ELSE
			TPWrite "Unknown data received from Axis...";
		ENDIF
    ENDTRAP
ENDMODULE