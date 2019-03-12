MODULE Messaging

    RECORD SD
        string MoveMethod;
        pos RobTarg;
        orient Orientation;
        pose ToolFrame;
    ENDRECORD

!    RECORD SD
!        string MoveMethod;
!        robjoint JointTarg;
!        pos RobTarg;
!        orient Orientation;
!        num TCPSpeed;
!        num ReorSpeed;
!        pose ToolFrame;
!    ENDRECORD

    PERS tooldata StreamingTool:=[TRUE,[[434.049,51.8079,260.293],[0.32432,0.52507,0.52248,0.58833]],[1,[0,0,1E-04],[1,0,0,0],0,0,0]];
    VAR speeddata testTCPSpeed := [ 100, 20, 200, 15 ]; ! Custom speed object for online testing.
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
            If MyData.MoveMethod="Linear" THEN
                StorePath;
!                MoveL [MyData.RobTarg,MyData.Orientation,[0,0,0,0],[9E9,9E9,9E9,9E9,9E9,9E9]],testTCPSpeed,z1,StreamingTool;
                MoveL [MyData.RobTarg,MyData.Orientation,[0,0,0,0],[9E9,9E9,9E9,9E9,9E9,9E9]],v50,z5, tool0 \Wobj:=wobj0;
                RestoPath;               
            ENDIF
        ENDIF



        
!        IF header.datatype="SD" THEN

!            RMQGetMsgData msg,MyData;
!            StreamingTool.tframe:=MyData.ToolFrame;

!            If MyData.MoveMethod="MJ" THEN
!                StorePath;
!                MoveAbsJ [MyData.JointTarg,[0,9E9,9E9,9E9,9E9,9E9]],testTCPSpeed,z1,StreamingTool\Wobj:=Wobj0;
!                RestoPath;

!            ELSEIF MyData.MoveMethod="ML" THEN
!                StorePath;
!                MoveL [MyData.RobTarg,MyData.Orientation,[0,0,0,0],[0,9E9,9E9,9E9,9E9,9E9]],testTCPSpeed,z1,StreamingTool\WObj:=Wobj0;
!                RestoPath;

!            ENDIF
!        ELSE
!            TPWrite "Unknown message data received...";
!        ENDIF

    ENDTRAP

ENDMODULE
