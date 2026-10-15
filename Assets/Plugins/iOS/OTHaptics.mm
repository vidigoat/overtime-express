#import <UIKit/UIKit.h>

// Taptic Engine feedback for Overtime: 0 selection, 1 medium, 2 heavy, 3 success, 4 error.
extern "C" void OTHaptic(int kind)
{
    static UISelectionFeedbackGenerator *sel = nil;
    static UIImpactFeedbackGenerator *med = nil, *heavy = nil;
    static UINotificationFeedbackGenerator *note = nil;
    if (!sel) { sel = [UISelectionFeedbackGenerator new]; med = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleMedium];
                heavy = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleHeavy]; note = [UINotificationFeedbackGenerator new]; }
    switch (kind) {
        case 0: [sel selectionChanged]; [sel prepare]; break;
        case 1: [med impactOccurred]; [med prepare]; break;
        case 2: [heavy impactOccurred]; [heavy prepare]; break;
        case 3: [note notificationOccurred:UINotificationFeedbackTypeSuccess]; break;
        default: [note notificationOccurred:UINotificationFeedbackTypeError]; break;
    }
}
