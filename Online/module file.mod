MODULE Messaging

    RECORD SD
        string MoveMethod;
        pose ToolFrame;
!        robjoint JointTarg;
!        pos RobTarg;
!        orient Orientation;
!        num TCPSpeed;
!        num ReorSpeed;
!        pose ToolFrame;
    ENDRECORD

    PERS tooldata StreamingTool:=[TRUE,[[0,0,0],[1,0,0,0]],[1,[0,0,0.0001],[1,0,0,0],0,0,0]];
    VAR speeddata testTCPSpeed := [ 100, 20, 200, 15 ]; ! Custom speed object for online testing.
    VAR bool flag:=FALSE;
    VAR intnum connectionNumber;
    VAR SD MyData;

    PROC Main()
		DeleteTrap;
		AddTrap;
        WHILE flag=FALSE DO
            ! Idle wait for connection.
            WaitTime 0.01;
        ENDWHILE

        TPWrite "RMQ message received from server. Acknowledgement sent.";
        EXIT;
    ENDPROC

	PROC DeleteTrap()
		IDelete connectionNumber; 
	ENDPROC

	PROC AddTrap()
		CONNECT connectionNumber WITH Process;
        IRMQMessage MyData,connectionNumber;
	ENDPROC

    TRAP Process
        VAR rmqmessage msg;
        VAR rmqheader header;
        VAR rmqslot rabclient;
        VAR num userdef;
        VAR string ack:="Message received from GH.";

        RMQGetMessage msg;
        RMQGetMsgHeader msg\Header:=header\SenderId:=rabclient\UserDef:=userdef;
        RMQSendMessage rabclient,ack;

        IF header.datatype="SD" THEN

            RMQGetMsgData msg,MyData;
            StreamingTool.tframe:=MyData.ToolFrame;

            If MyData.MoveMethod="0" THEN
                
                StorePath;
                MoveL [MyData.ToolFrame.trans,MyData.ToolFrame.rot,[0,0,0,0],[9E9,9E9,9E9,9E9,9E9,9E9]],testTCPSpeed,z1,StreamingTool;
                RestoPath;               

            ENDIF
        ELSE
            TPWrite "Unknown message data received...";
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