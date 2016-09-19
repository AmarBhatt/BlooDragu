#ifndef PWM_H_
#define PWM_H_

void parseCycles(char *buffer);
//void setDutyCycle(unsigned int dutyCycle, int joint);
void setDutyCycles(void);
void InitPWM(void);
void PWM_ISR(void);
char * getPositions(void);


#endif /* PWM_H_ */
