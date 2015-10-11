using System;
using System.Text;
using System.Collections.Generic;
namespace Microsoft.Bek.Tests {
public static class HtmlEncode{

static int Bits(int m, int n, int c){return ((c >> n) & ~(-2 << (m - n)));}

public static string Apply(string input){
var output = new char[(input.Length * 8) + 0];
bool r0 = false;int r1 = 0;
int pos = 0;
var chars = input.ToCharArray();
for (int i = 0; i < chars.Length; i++){
int c = (int)chars[i];
  if (!r0) {
    if ((((0x20<=c)&&(Bits(15,6,c)==0)&&(Bits(5,0,c)<=0x21))||((0x23<=c)&&(Bits(15,6,c)==0)&&(Bits(5,0,c)<=0x25))||((0x28<=c)&&(Bits(15,6,c)==0)&&(Bits(5,0,c)<=0x3B))||(c==0x3D)||((0x3F<=c)&&(Bits(15,7,c)==0)&&(Bits(6,0,c)<=0x7E))||((0xA1<=c)&&(Bits(15,8,c)==0)&&(Bits(7,0,c)<=0xAC))||((0xAE<=c)&&(Bits(15,10,c)==0)&&(Bits(9,0,c)<=0x36F)))) {
      output[pos++] = ((char)c);
    } else {
      if ((c==0x22)) {
        output[pos++] = ((char)0x26);output[pos++] = ((char)0x71);output[pos++] = ((char)0x75);output[pos++] = ((char)0x6F);output[pos++] = ((char)0x74);output[pos++] = ((char)0x3B);
      } else {
        if ((c==0x26)) {
          output[pos++] = ((char)0x26);output[pos++] = ((char)0x61);output[pos++] = ((char)0x6D);output[pos++] = ((char)0x70);output[pos++] = ((char)0x3B);
        } else {
          if ((c==0x3C)) {
            output[pos++] = ((char)0x26);output[pos++] = ((char)0x6C);output[pos++] = ((char)0x74);output[pos++] = ((char)0x3B);
          } else {
            if ((c==0x3E)) {
              output[pos++] = ((char)0x26);output[pos++] = ((char)0x67);output[pos++] = ((char)0x74);output[pos++] = ((char)0x3B);
            } else {
              if ((0x10>c)) {
                output[pos++] = ((char)0x26);output[pos++] = ((char)0x23);output[pos++] = ((char)0x58);output[pos++] = ((char)((c&0xF)+((c&0xF)<=9 ? 48 : 55)));output[pos++] = ((char)0x3B);
              } else {
                if ((0x100>c)) {
                  output[pos++] = ((char)0x26);output[pos++] = ((char)0x23);output[pos++] = ((char)0x58);output[pos++] = ((char)(((c>>4)&0xF)+(((c>>4)&0xF)<=9 ? 48 : 55)));output[pos++] = ((char)((c&0xF)+((c&0xF)<=9 ? 48 : 55)));output[pos++] = ((char)0x3B);
                } else {
                  if ((0x1000>c)) {
                    output[pos++] = ((char)0x26);output[pos++] = ((char)0x23);output[pos++] = ((char)0x58);output[pos++] = ((char)(((c>>8)&0xF)+(((c>>8)&0xF)<=9 ? 48 : 55)));output[pos++] = ((char)(((c>>4)&0xF)+(((c>>4)&0xF)<=9 ? 48 : 55)));output[pos++] = ((char)((c&0xF)+((c&0xF)<=9 ? 48 : 55)));output[pos++] = ((char)0x3B);
                  } else {
                    if (((0xD800<=c)&&(c<=0xDBFF))) {
                      if ((Bits(9,6,c)==0xF)) {
                        output[pos++] = ((char)0x26);output[pos++] = ((char)0x23);output[pos++] = ((char)0x58);output[pos++] = ((char)0x31);output[pos++] = ((char)0x30);output[pos++] = ((char)((Bits(15,2,c)&0xF)+((Bits(15,2,c)&0xF)<=9 ? 48 : 55)));
                        r0 = true;r1 = Bits(1,0,c);
                      } else {
                        output[pos++] = ((char)0x26);output[pos++] = ((char)0x23);output[pos++] = ((char)0x58);output[pos++] = ((char)(((1+Bits(15,6,c))&0xF)+(((1+Bits(15,6,c))&0xF)<=9 ? 48 : 55)));output[pos++] = ((char)((Bits(15,2,c)&0xF)+((Bits(15,2,c)&0xF)<=9 ? 48 : 55)));
                        r0 = true;r1 = Bits(1,0,c);
                      }
                    } else {
                      if ((((0xDC00<=c)&&(c<=0xDFFF))||(c==0xFFFF)||(c==0xFFFE))) {
                        throw new Exception("HtmlEncode");
                      } else {
                        output[pos++] = ((char)0x26);output[pos++] = ((char)0x23);output[pos++] = ((char)0x58);output[pos++] = ((char)(((c>>12)&0xF)+(((c>>12)&0xF)<=9 ? 48 : 55)));output[pos++] = ((char)(((c>>8)&0xF)+(((c>>8)&0xF)<=9 ? 48 : 55)));output[pos++] = ((char)(((c>>4)&0xF)+(((c>>4)&0xF)<=9 ? 48 : 55)));output[pos++] = ((char)((c&0xF)+((c&0xF)<=9 ? 48 : 55)));output[pos++] = ((char)0x3B);
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
  } else {
    if (((0xDC00<=c)&&(c<=0xDFFF))) {
      output[pos++] = ((char)((((Bits(13,0,r1)<<2)|Bits(9,8,c))&0xF)+((((Bits(13,0,r1)<<2)|Bits(9,8,c))&0xF)<=9 ? 48 : 55)));output[pos++] = ((char)(((c>>4)&0xF)+(((c>>4)&0xF)<=9 ? 48 : 55)));output[pos++] = ((char)((c&0xF)+((c&0xF)<=9 ? 48 : 55)));output[pos++] = ((char)0x3B);
      r0 = false;r1 = 0;
    } else {
      throw new Exception("HtmlEncode");
    }
  }
}
  if (r0) {
    throw new Exception("HtmlEncode");
  } else {
    
  }
return new String(output, 0, pos);
}
}
}
