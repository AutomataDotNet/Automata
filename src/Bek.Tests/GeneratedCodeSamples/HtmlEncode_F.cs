using System;
using System.Text;
using System.Collections.Generic;
namespace Microsoft.Bek.Tests {
public static class HtmlEncode_F{

static int Bits(int m, int n, int c){return ((c >> n) & ~(-2 << (m - n)));}

public static string Apply(string input){
var output = new char[(input.Length * 8) + 0];
int state = 0;
int pos = 0;
var chars = input.ToCharArray();
for (int i = 0; i < chars.Length; i++){
int c = (int)chars[i];
switch (state){
case (0): {
  if ((((0x20<=c)&&(Bits(15,6,c)==0)&&(Bits(5,0,c)<=0x21))||((0x23<=c)&&(Bits(15,6,c)==0)&&(Bits(5,0,c)<=0x25))||((0x28<=c)&&(Bits(15,6,c)==0)&&(Bits(5,0,c)<=0x3B))||(c==0x3D)||((0x3F<=c)&&(Bits(15,7,c)==0)&&(Bits(6,0,c)<=0x7E))||((0xA1<=c)&&(Bits(15,8,c)==0)&&(Bits(7,0,c)<=0xAC))||((0xAE<=c)&&(Bits(15,10,c)==0)&&(Bits(9,0,c)<=0x36F)))) {
    output[pos++] = ((char)c);
    state = 0;
  } else {
    if ((c==0x22)) {
      output[pos++] = ((char)0x26);output[pos++] = ((char)0x71);output[pos++] = ((char)0x75);output[pos++] = ((char)0x6F);output[pos++] = ((char)0x74);output[pos++] = ((char)0x3B);
      state = 0;
    } else {
      if ((c==0x26)) {
        output[pos++] = ((char)0x26);output[pos++] = ((char)0x61);output[pos++] = ((char)0x6D);output[pos++] = ((char)0x70);output[pos++] = ((char)0x3B);
        state = 0;
      } else {
        if ((c==0x3C)) {
          output[pos++] = ((char)0x26);output[pos++] = ((char)0x6C);output[pos++] = ((char)0x74);output[pos++] = ((char)0x3B);
          state = 0;
        } else {
          if ((c==0x3E)) {
            output[pos++] = ((char)0x26);output[pos++] = ((char)0x67);output[pos++] = ((char)0x74);output[pos++] = ((char)0x3B);
            state = 0;
          } else {
            if ((0x10>c)) {
              output[pos++] = ((char)0x26);output[pos++] = ((char)0x23);output[pos++] = ((char)0x58);output[pos++] = ((char)((c&0xF)+((c&0xF)<=9 ? 48 : 55)));output[pos++] = ((char)0x3B);
              state = 0;
            } else {
              if ((0x100>c)) {
                output[pos++] = ((char)0x26);output[pos++] = ((char)0x23);output[pos++] = ((char)0x58);output[pos++] = ((char)(((c>>4)&0xF)+(((c>>4)&0xF)<=9 ? 48 : 55)));output[pos++] = ((char)((c&0xF)+((c&0xF)<=9 ? 48 : 55)));output[pos++] = ((char)0x3B);
                state = 0;
              } else {
                if ((0x1000>c)) {
                  output[pos++] = ((char)0x26);output[pos++] = ((char)0x23);output[pos++] = ((char)0x58);output[pos++] = ((char)(((c>>8)&0xF)+(((c>>8)&0xF)<=9 ? 48 : 55)));output[pos++] = ((char)(((c>>4)&0xF)+(((c>>4)&0xF)<=9 ? 48 : 55)));output[pos++] = ((char)((c&0xF)+((c&0xF)<=9 ? 48 : 55)));output[pos++] = ((char)0x3B);
                  state = 0;
                } else {
                  if (((0xD800<=c)&&(c<=0xDBFF))) {
                    if ((Bits(9,6,c)==0xF)) {
                      if ((Bits(1,0,c)==0)) {
                        output[pos++] = ((char)0x26);output[pos++] = ((char)0x23);output[pos++] = ((char)0x58);output[pos++] = ((char)0x31);output[pos++] = ((char)0x30);output[pos++] = ((char)((Bits(15,2,c)&0xF)+((Bits(15,2,c)&0xF)<=9 ? 48 : 55)));
                        state = 4;
                      } else {
                        if ((Bits(1,0,c)==2)) {
                          output[pos++] = ((char)0x26);output[pos++] = ((char)0x23);output[pos++] = ((char)0x58);output[pos++] = ((char)0x31);output[pos++] = ((char)0x30);output[pos++] = ((char)((Bits(15,2,c)&0xF)+((Bits(15,2,c)&0xF)<=9 ? 48 : 55)));
                          state = 3;
                        } else {
                          if ((Bits(1,0,c)==3)) {
                            output[pos++] = ((char)0x26);output[pos++] = ((char)0x23);output[pos++] = ((char)0x58);output[pos++] = ((char)0x31);output[pos++] = ((char)0x30);output[pos++] = ((char)((Bits(15,2,c)&0xF)+((Bits(15,2,c)&0xF)<=9 ? 48 : 55)));
                            state = 2;
                          } else {
                            output[pos++] = ((char)0x26);output[pos++] = ((char)0x23);output[pos++] = ((char)0x58);output[pos++] = ((char)0x31);output[pos++] = ((char)0x30);output[pos++] = ((char)((Bits(15,2,c)&0xF)+((Bits(15,2,c)&0xF)<=9 ? 48 : 55)));
                            state = 1;
                          }
                        }
                      }
                    } else {
                      if ((Bits(1,0,c)==0)) {
                        output[pos++] = ((char)0x26);output[pos++] = ((char)0x23);output[pos++] = ((char)0x58);output[pos++] = ((char)(((1+Bits(15,6,c))&0xF)+(((1+Bits(15,6,c))&0xF)<=9 ? 48 : 55)));output[pos++] = ((char)((Bits(15,2,c)&0xF)+((Bits(15,2,c)&0xF)<=9 ? 48 : 55)));
                        state = 4;
                      } else {
                        if ((Bits(1,0,c)==2)) {
                          output[pos++] = ((char)0x26);output[pos++] = ((char)0x23);output[pos++] = ((char)0x58);output[pos++] = ((char)(((1+Bits(15,6,c))&0xF)+(((1+Bits(15,6,c))&0xF)<=9 ? 48 : 55)));output[pos++] = ((char)((Bits(15,2,c)&0xF)+((Bits(15,2,c)&0xF)<=9 ? 48 : 55)));
                          state = 3;
                        } else {
                          if ((Bits(1,0,c)==3)) {
                            output[pos++] = ((char)0x26);output[pos++] = ((char)0x23);output[pos++] = ((char)0x58);output[pos++] = ((char)(((1+Bits(15,6,c))&0xF)+(((1+Bits(15,6,c))&0xF)<=9 ? 48 : 55)));output[pos++] = ((char)((Bits(15,2,c)&0xF)+((Bits(15,2,c)&0xF)<=9 ? 48 : 55)));
                            state = 2;
                          } else {
                            output[pos++] = ((char)0x26);output[pos++] = ((char)0x23);output[pos++] = ((char)0x58);output[pos++] = ((char)(((1+Bits(15,6,c))&0xF)+(((1+Bits(15,6,c))&0xF)<=9 ? 48 : 55)));output[pos++] = ((char)((Bits(15,2,c)&0xF)+((Bits(15,2,c)&0xF)<=9 ? 48 : 55)));
                            state = 1;
                          }
                        }
                      }
                    }
                  } else {
                    if ((((0xDC00<=c)&&(c<=0xDFFF))||(c==0xFFFF)||(c==0xFFFE))) {
                      throw new Exception("HtmlEncode_F");
                    } else {
                      output[pos++] = ((char)0x26);output[pos++] = ((char)0x23);output[pos++] = ((char)0x58);output[pos++] = ((char)(((c>>12)&0xF)+(((c>>12)&0xF)<=9 ? 48 : 55)));output[pos++] = ((char)(((c>>8)&0xF)+(((c>>8)&0xF)<=9 ? 48 : 55)));output[pos++] = ((char)(((c>>4)&0xF)+(((c>>4)&0xF)<=9 ? 48 : 55)));output[pos++] = ((char)((c&0xF)+((c&0xF)<=9 ? 48 : 55)));output[pos++] = ((char)0x3B);
                      state = 0;
                    }
                  }
                }
              }
            }
          }
        }
      }
    }
  }
break;}
case (4): {
  if (((0xDC00<=c)&&(c<=0xDFFF))) {
    output[pos++] = ((char)((Bits(9,8,c)&0xF)+((Bits(9,8,c)&0xF)<=9 ? 48 : 55)));output[pos++] = ((char)(((c>>4)&0xF)+(((c>>4)&0xF)<=9 ? 48 : 55)));output[pos++] = ((char)((c&0xF)+((c&0xF)<=9 ? 48 : 55)));output[pos++] = ((char)0x3B);
    state = 0;
  } else {
    throw new Exception("HtmlEncode_F");
  }
break;}
case (3): {
  if (((0xDC00<=c)&&(c<=0xDFFF))) {
    output[pos++] = ((char)(((8|Bits(9,8,c))&0xF)+(((8|Bits(9,8,c))&0xF)<=9 ? 48 : 55)));output[pos++] = ((char)(((c>>4)&0xF)+(((c>>4)&0xF)<=9 ? 48 : 55)));output[pos++] = ((char)((c&0xF)+((c&0xF)<=9 ? 48 : 55)));output[pos++] = ((char)0x3B);
    state = 0;
  } else {
    throw new Exception("HtmlEncode_F");
  }
break;}
case (2): {
  if (((0xDC00<=c)&&(c<=0xDFFF))) {
    output[pos++] = ((char)(((0xC|Bits(9,8,c))&0xF)+(((0xC|Bits(9,8,c))&0xF)<=9 ? 48 : 55)));output[pos++] = ((char)(((c>>4)&0xF)+(((c>>4)&0xF)<=9 ? 48 : 55)));output[pos++] = ((char)((c&0xF)+((c&0xF)<=9 ? 48 : 55)));output[pos++] = ((char)0x3B);
    state = 0;
  } else {
    throw new Exception("HtmlEncode_F");
  }
break;}
case (1): {
  if (((0xDC00<=c)&&(c<=0xDFFF))) {
    output[pos++] = ((char)(((4|Bits(9,8,c))&0xF)+(((4|Bits(9,8,c))&0xF)<=9 ? 48 : 55)));output[pos++] = ((char)(((c>>4)&0xF)+(((c>>4)&0xF)<=9 ? 48 : 55)));output[pos++] = ((char)((c&0xF)+((c&0xF)<=9 ? 48 : 55)));output[pos++] = ((char)0x3B);
    state = 0;
  } else {
    throw new Exception("HtmlEncode_F");
  }
break;}
}}
if (state == 0){
  
  state = 0;
}
else if (state == 4){
  throw new Exception("HtmlEncode_F");
}
else if (state == 3){
  throw new Exception("HtmlEncode_F");
}
else if (state == 2){
  throw new Exception("HtmlEncode_F");
}
else if (state == 1){
  throw new Exception("HtmlEncode_F");
}
return new String(output, 0, pos);
}
}
}
