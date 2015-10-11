using System;
using System.Text;
using System.Collections.Generic;

namespace Microsoft.Bek.Tests
{
public class DecodeDigitPairs : Microsoft.Automata.IDeterministicFiniteTransducer
{
static int Bits(int m, int n, int c){return ((c >> n) & ~(-2 << (m - n)));}

public static string Apply(string input){
var output = new StringBuilder(input.Length >= 16384 ? input.Length : (int)(Math.Min(16384, (long)input.Length * 2)));
int state = 0;
for (int i = 0; i < input.Length; i++){
int c = (int)input[i];
switch (state){
case (0): {
  if (((0x36<=c)&&(Bits(15,6,c)==0)&&(Bits(5,0,c)<=0x37))) {
    if ((c==0x36)) {
      
      state = 2;
    } else {
      
      state = 1;
    }
  } else {
    output.Append((char)c);
    state = 0;
  }
break;}
case (2): {
  if (((0x36<=c)&&(Bits(15,6,c)==0)&&(Bits(5,0,c)<=0x37))) {
    output.Append((char)(0x3C+Bits(3,0,c)));
    state = 0;
  } else {
    output.Append(new char[]{(char)0x36, (char)c});
    state = 0;
  }
break;}
case (1): {
  if (((0x36<=c)&&(Bits(15,6,c)==0)&&(Bits(5,0,c)<=0x37))) {
    output.Append((char)(0x46+Bits(3,0,c)));
    state = 0;
  } else {
    output.Append(new char[]{(char)0x37, (char)c});
    state = 0;
  }
break;}
default: {
  throw new Exception("DecodeDigitPairs");
}
}
}
switch (state){
case (0): {
  
break;}
case (2): {
  output.Append((char)0x36);
break;}
case (1): {
  output.Append((char)0x37);
break;}
default: {
  throw new Exception("DecodeDigitPairs");
}
}
return output.ToString();
}

public static IEnumerable<char> Transduce(IEnumerable<char> input){
var encoder = new DecodeDigitPairs();
int state = encoder.q0;
foreach (char c in input)
{
  foreach (char d in encoder.Psi(state, (int)c))
    yield return d;
  state = encoder.Delta(state, (int)c);
}
if (encoder.F.Contains(state))
{
  foreach (char d in encoder.Psi(state, -1))
    yield return d;
}
else
  throw new Exception("DecodeDigitPairs");
}


public ICollection<int> Q { get { return new int[]{ 0, 2, 1, -1 }; } }

public int q0 { get { return 0 ; } }

public ICollection<int> F { get { return new int[]{ 0, 2, 1,  }; } }

public IEnumerable<char> Sigma { get { for (char i = char.MinValue; i <= char.MaxValue; i++) yield return i; } }

public int Delta(int state, int c){
if (c < 0) return state;
switch (state){
case (0): {
  if (((0x36<=c)&&(Bits(15,6,c)==0)&&(Bits(5,0,c)<=0x37))) {
    if ((c==0x36)) {
      
      state = 2;
    } else {
      
      state = 1;
    }
  } else {
    
    state = 0;
  }
break;}
case (2): {
  if (((0x36<=c)&&(Bits(15,6,c)==0)&&(Bits(5,0,c)<=0x37))) {
    
    state = 0;
  } else {
    
    state = 0;
  }
break;}
case (1): {
  if (((0x36<=c)&&(Bits(15,6,c)==0)&&(Bits(5,0,c)<=0x37))) {
    
    state = 0;
  } else {
    
    state = 0;
  }
break;}
default: {
state = -1;
break;
}
}
return state;
}

public IEnumerable<char> Psi(int state, int c){
if (c >= 0) {
switch (state){
case (0): {
  if (((0x36<=c)&&(Bits(15,6,c)==0)&&(Bits(5,0,c)<=0x37))) {
    if ((c==0x36)) {
      yield break;
    } else {
      yield break;
    }
  } else {
    yield return ((char)c); 
  }
break;}
case (2): {
  if (((0x36<=c)&&(Bits(15,6,c)==0)&&(Bits(5,0,c)<=0x37))) {
    yield return ((char)(0x3C+Bits(3,0,c))); 
  } else {
    yield return ((char)0x36); yield return ((char)c); 
  }
break;}
case (1): {
  if (((0x36<=c)&&(Bits(15,6,c)==0)&&(Bits(5,0,c)<=0x37))) {
    yield return ((char)(0x46+Bits(3,0,c))); 
  } else {
    yield return ((char)0x37); yield return ((char)c); 
  }
break;}
default: {
yield break;
}
}
}
else {
switch (state){
case (0): {
  yield break;
break;}
case (2): {
  yield return ((char)0x36); 
break;}
case (1): {
  yield return ((char)0x37); 
break;}
default: {
yield break;
}
}
}
}

public string Psi2(int state, int c){
if (c >= 0) {
switch (state){
case (0): {
  if (((0x36<=c)&&(Bits(15,6,c)==0)&&(Bits(5,0,c)<=0x37))) {
    if ((c==0x36)) {
      return "";
    } else {
      return "";
    }
  } else {
    return new String(new char[] {((char)c),});
  }
}
case (2): {
  if (((0x36<=c)&&(Bits(15,6,c)==0)&&(Bits(5,0,c)<=0x37))) {
    return new String(new char[] {((char)(0x3C+Bits(3,0,c))),});
  } else {
    return new String(new char[] {((char)0x36),((char)c),});
  }
}
case (1): {
  if (((0x36<=c)&&(Bits(15,6,c)==0)&&(Bits(5,0,c)<=0x37))) {
    return new String(new char[] {((char)(0x46+Bits(3,0,c))),});
  } else {
    return new String(new char[] {((char)0x37),((char)c),});
  }
}
default: {
return "";
}
}
}
else {
switch (state){
case (0): {
  return "";
}
case (2): {
  return new String(new char[] {((char)0x36),});
}
case (1): {
  return new String(new char[] {((char)0x37),});
}
default: {
return "";
}
}
}
}
}
}
