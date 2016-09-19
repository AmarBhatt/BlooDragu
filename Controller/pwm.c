/*
 * Pulse-Width-Modulation Code for K64
 * 
 */
#include "MK64F12.h"
#include "pwm.h"
#include <stdio.h>

/*From clock setup 0 in system_MK64f12.c*/
#define DEFAULT_SYSTEM_CLOCK 	20485760u /* Default System clock value */
#define CLOCK									20485760u
#define PWM_FREQUENCY					50
#define SERVO_FREQUENCY       6400
// In order to have a 16bit counter value the clock has to be divided by 8,
// but some precision is lost. So an "adjustment" term is necessary in this
// case. Adding 1500 brings the clock close to 50Hz
// FTM0_MOD_VALUE = 52714.4 (Decimal is truncated).
#define FTM0_MOD_VALUE			(CLOCK/8/PWM_FREQUENCY + 1500)

static volatile unsigned int PWMTick = 0;
static unsigned int p[6];
static char ascii_positions[80];

void parseCycles(char *buffer){
	int i;
	sscanf(buffer,"%d %d %d %d %d %d",&p[0],&p[1],&p[2],&p[3],&p[4],&p[5]);
	// Lame value checking
	for(i=0;i<6;i++){
		if(p[i] > 100){
			p[i] = 100;
		}
	}
}

//void setDutyCycle(unsigned int dutyCycle, int joint)
void setDutyCycles(void)
{	
	FTM0_C2V = (uint16_t) ((FTM0_MOD_VALUE * p[0]) / 100);
	FTM0_C3V = (uint16_t) ((FTM0_MOD_VALUE * p[1]) / 100);
	FTM0_C7V = (uint16_t) ((FTM0_MOD_VALUE * p[2]) / 100);
	FTM0_C6V = (uint16_t) ((FTM0_MOD_VALUE * p[3]) / 100);
	FTM0_C1V = (uint16_t) ((FTM0_MOD_VALUE * p[4]) / 100);
	FTM0_C4V = (uint16_t) ((FTM0_MOD_VALUE * p[5]) / 100);
	
	// legacy code
  // Calculate the new cutoff value
	/*
	uint16_t mod = (uint16_t) ((FTM0_MOD_VALUE * dutyCycle) / 100);
    switch(joint) {
        case 1 :
            FTM0_C2V = mod;
        case 2 :
            FTM0_C3V = mod;
        case 3 :
            FTM0_C7V = mod;
        case 4 :
            FTM0_C6V = mod;
        case 5 :
            FTM0_C1V = mod;
        case 6 :
            FTM0_C4V = mod;
            break;
        default : 
            break;
    }
	*/
	// Update the clock to the new frequency
	//FTM0_MOD = FTM0_MOD_VALUE;
}

/*
 * Change the Motor Duty Cycle and Frequency
 * @param DutyCycle (0 to 100)
 * @param motorRight 0 for Right motor, 1 for Left motor
 * @param dir: 1 for forward, else backwards 
 */

/*
 * Initialize the FlexTimer for PWM
 */
void InitPWM(void){
	int i;
	// 12.2.13 Enable the Clock to the FTM0 Module
	SIM_SCGC6 |= SIM_SCGC6_FTM0_MASK;
	
	// Enable clock on PORT A and C
	SIM_SCGC5 |= SIM_SCGC5_PORTA_MASK | SIM_SCGC5_PORTC_MASK | SIM_SCGC5_PORTD_MASK;
	
	// 11.4.1 Route the output of TPM channel 0 to the pins
	// Use drive strength enable flag to high drive strength
	//These port/pins may need to be updated for the K64 <Yes, they do. Here are two that work.>
  PORTC_PCR3  = PORT_PCR_MUX(4)  | PORT_PCR_DSE_MASK; //FTM0 Ch2 -> PTC3 -> Servo 1
  PORTC_PCR4  = PORT_PCR_MUX(4)  | PORT_PCR_DSE_MASK; //FTM0 Ch3 -> PTC4 -> Servo 2
	PORTA_PCR2  = PORT_PCR_MUX(3)  | PORT_PCR_DSE_MASK; //FTM0 Ch7 -> PTA2 -> Servo 3
	PORTA_PCR1  = PORT_PCR_MUX(3)  | PORT_PCR_DSE_MASK; //FTM0 Ch6 -> PTA1 -> Servo 4
  PORTC_PCR2  = PORT_PCR_MUX(4)  | PORT_PCR_DSE_MASK; //FTM0 Ch1 -> PTC2 -> Servo 5
  PORTD_PCR4  = PORT_PCR_MUX(4)  | PORT_PCR_DSE_MASK; //FTM0 Ch4 -> PTD4 -> Servo 6
  //PORTA_PCR0  = PORT_PCR_MUX(3)  | PORT_PCR_DSE_MASK; //FTM0 Ch5 -> PTA0 -> Servo 6
	
	// 39.3.10 Disable Write Protection
	FTM0_MODE |= FTM_MODE_WPDIS_MASK;
	
	// 39.3.4 FTM Counter Value
	// Initialize the CNT to 0 before writing to MOD
	FTM0_CNT = 0;
	
	// 39.3.8 Set the Counter Initial Value to 0
	FTM0_CNTIN = 0;
	
	// 39.3.5 Set the Modulo register
	FTM0_MOD = FTM0_MOD_VALUE;

	//FTM0->MOD = (DEFAULT_SYSTEM_CLOCK/(1<<7))/1000;

	// 39.3.6 Set the Status and Control of both channels
	// Used to configure mode, edge and level selection
	// See Table 39-67,  Edge-aligned PWM, High-true pulses (clear out on match)
	FTM0_C3SC |= FTM_CnSC_MSB_MASK | FTM_CnSC_ELSB_MASK;
	FTM0_C3SC &= ~FTM_CnSC_ELSA_MASK;
	
	// See Table 39-67,  Edge-aligned PWM, Low-true pulses (clear out on match)
	FTM0_C2SC |= FTM_CnSC_MSB_MASK | FTM_CnSC_ELSB_MASK;
	FTM0_C2SC &= ~FTM_CnSC_ELSA_MASK;
	
	FTM0_C7SC |= FTM_CnSC_MSB_MASK | FTM_CnSC_ELSB_MASK;
	FTM0_C7SC &= ~FTM_CnSC_ELSA_MASK;
	
	FTM0_C6SC |= FTM_CnSC_MSB_MASK | FTM_CnSC_ELSB_MASK;
	FTM0_C6SC &= ~FTM_CnSC_ELSA_MASK;
	
	FTM0_C1SC |= FTM_CnSC_MSB_MASK | FTM_CnSC_ELSB_MASK;
	FTM0_C1SC &= ~FTM_CnSC_ELSA_MASK;
    
	FTM0_C4SC |= FTM_CnSC_MSB_MASK | FTM_CnSC_ELSB_MASK;
	FTM0_C4SC &= ~FTM_CnSC_ELSA_MASK;    
	
	// 39.3.3 FTM Setup
	// Set prescale value to 1 
	// Chose system clock source
	// Timer Overflow Interrupt Enable
	FTM0_SC = FTM_SC_PS(3) | FTM_SC_CLKS(1) | FTM_SC_TOIE_MASK;

	// Enable Interrupt Vector for FTM
    NVIC_EnableIRQ(FTM0_IRQn);
	
	// Initialize PWM signals
	for(i = 0;i<=5;i++){
		p[i] = 50;
	}
	
}

char * getPositions(void){
	sprintf(ascii_positions, "1:%d%% 2:%d%% 3:%d%% 4:%d%% 5:%d%% 6:%d%%\n\r", p[0],p[1],p[2],p[3],p[4],p[5]);
	return ascii_positions;
}

/*OK to remove this ISR?*/
void FTM0_IRQHandler(void){ //For FTM timer

  FTM0_SC &= ~FTM_SC_TOF_MASK;
  
	//if motor tick less than 255 count up... 
	//if (PWMTick < 0xff)
	//	PWMTick++;
  
	
}
