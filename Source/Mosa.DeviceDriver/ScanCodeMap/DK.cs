// Copyright (c) MOSA Project. Licensed under the New BSD License.

using Mosa.DeviceSystem;

namespace Mosa.DeviceDriver.ScanCodeMap
{
    public class DK : IScanCodeMap
    {
        private enum KeyState {Normal,Escaped,Escaped2,EscapedBreak};
        
        private KeyState keyState;
        
        public DK(){
            keyState= KeyState.Normal;
        }
        public KeyEvent ConvertScanCode(byte scancode)
        {
            KeyEvent key = new KeyEvent();
            if(scancode ==0)
                return key;
            if(keyState == KeyState.Normal)
			{
				if(scancode==0xE0)
				{
					keyState = KeyState.Escaped;
					return key;
				}
				if((scancode &0x80)!=0)
					key.KeyPress = KeyEvent.Press.Break;
				else
					key.KeyPress = KeyEvent.Press.Make;
				key.KeyType= KeyType.RegularKey;
				switch(scancode)
				{
					case   1: key.Character = (char)27;break;
					case   2: key.Character = '1'; break;
					case   3: key.Character = '2'; break;
					case   4: key.Character = '3'; break;
					case   5: key.Character = '4'; break;
					case   6: key.Character = '5'; break;
					case   7: key.Character = '6'; break;
					case   8: key.Character = '7'; break;
					case   9: key.Character = '8'; break;
					case  10: key.Character = '9'; break;
					case  11: key.Character = '0'; break;
					case  12: key.Character = '+'; break;
					case  13: key.Character = '='; break;
					case  14: key.Character = '\b';break;
					
				}
			}
                
           return null;
        }
    }
}