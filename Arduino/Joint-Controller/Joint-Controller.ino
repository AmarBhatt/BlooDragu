
// zoomkat 10-22-11 serial servo test
// type servo position 0 to 180 in serial monitor
// or for writeMicroseconds, use a value like 1500
// for IDE 0022 and later
// Powering a servo from the arduino usually *DOES NOT WORK*.

String readString;
#include <Servo.h> 
Servo myservoA;  // create servo object to control a servo 
Servo myservoB;  // create servo object to control a servo 
Servo myservoC;  // create servo object to control a servo 
Servo myservoD;  // create servo object to control a servo 
Servo myservoE;  // create servo object to control a servo 
Servo myservoF;  // create servo object to control a servo 
int a_state = 90;
int b_state = 90;
int c_state = 90;
int d_state = 90;
int e_state = 90;
int f_state = 90;
char motor;
int val;

void setup() {
  Serial.begin(9600);
  //myservoA.writeMicroseconds(1500); //set initial servo position if desired
  myservoA.attach(8);  //the pin for the servo control 
  myservoB.attach(9);
  myservoC.attach(10);
  myservoD.attach(11);  
  myservoE.attach(12);
  myservoF.attach(13);
  Serial.println("servo-test-22-dual-input"); // so I can keep track of what is loaded
  myservoA.write(90);  //the pin for the servo control 
  myservoB.write(90);
  myservoC.write(90);
  myservoD.write(90);  
  myservoE.write(90);
  myservoF.write(90);
}

void loop() {
  while (Serial.available()) {
    char c = Serial.read();  //gets one byte from serial buffer
    readString += c; //makes the string readString
    delay(2);  //slow looping to allow buffer to fill with next character
  }
  if(readString.length() > 0) {
    
    motor = readString.charAt(0);
    if(motor != 's'){
      readString.replace(motor,' ');
      readString.trim();
      val = constrain(readString.toInt(),0,180);
    }
    
    Serial.print("Writing to Motor: ");
    Serial.print(motor);
    Serial.print(", Angle: ");
    Serial.println(val);
    
    switch (motor) {
        case 'A':
          myservoA.write(val);
          a_state = val;
          break;
        case 'B':
          myservoB.write(val);A180
          
          b_state = val;
          break;
        case 'C':
          myservoC.write(val);
          c_state = val;        
          break;
        case 'D':
          myservoD.write(val);
          d_state = val;
          break;
        case 'E':
          myservoE.write(val);
          e_state = val;
          break;
        case 'F':
          myservoF.write(val);
          f_state = val;
          break;
         case 'S':
           Serial.print("A: ");
           Serial.println(a_state);
           Serial.print("B: ");
           Serial.println(b_state);
           Serial.print("C: ");
           Serial.println(c_state);
           Serial.print("D: ");
           Serial.println(d_state);
           Serial.print("E: ");
           Serial.println(e_state);
           Serial.print("F: ");
           Serial.println(f_state);
           Serial.println(" ");
           break;
        default:
          Serial.println("Enter <A,B,C,D,E,F><angle> to access motor joints.");    
    }
  }
  readString=""; //empty for next input
//  if (readString.length() >0) {
//    Serial.println(readString);  //so you can see the captured string 
//    int n = readString.toInt();  //convert readString into a number
//
//    // auto select appropriate value, copied from someone elses code.
//    if(n >= 500)
//    {
//      Serial.print("writing Microseconds: ");
//      Serial.println(n);
//      myservoA.writeMicroseconds(n);
//    }
//    else
//    {   
//      Serial.print("writing Angle: ");
//      Serial.println(n);
//      myservoA.write(n);
//    }
//
//    readString=""; //empty for next input
//  } 
}

