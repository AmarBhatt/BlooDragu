/*
 * Author:  Luke Boudreau
 */

#include "MK64F12.h"
#include "uart.h"
#include "PWM.h"

void initialize(void);

int main(void)
{
	// Used to store serial input
	char ascii_duty_cyles[80];
	
	// Initialize UART and PWM
	initialize();

	// Print welcome over serial
	put("Running... \n\r");
	put(getPositions());
	
	// Get duty cycles
	for(;;){
		getCyclesSerial(&ascii_duty_cyles[0]);
		parseCycles(&ascii_duty_cyles[0]);
		setDutyCycles();
		put("\n\r");
		put(getPositions());
		put("\n\rDone\n\r");
	}

	//return 0;	//unnecessary
}

void initialize()
{
    int i;
	// Button start enabled
	// Enable clock for Port C PTC6 button
	SIM_SCGC5 |= SIM_SCGC5_PORTC_MASK; 
	
	// Configure the Mux for the button
	PORTC_PCR6 = PORT_PCR_MUX(1);
	GPIOC_PDDR &= ~(1<<6);

	// Initialize UART
	uart_init();	
	
	// Initialize the FlexTimer
	InitPWM();
    setDutyCycles();
    for(i=0;i<10;i++){
        put("\n\n\n\n\n\n\n\n\n\n");
    }
	put("Finished Initialization\n\r");
	

}
