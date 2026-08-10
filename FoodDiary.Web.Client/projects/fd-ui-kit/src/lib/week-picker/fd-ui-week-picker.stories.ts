import type { Meta, StoryObj } from '@storybook/angular';

import { fdUiAddLocalDays, fdUiStartOfLocalWeek } from '../date/fd-ui-date.utils';
import { FdUiWeekPickerComponent } from './fd-ui-week-picker';

const PREVIOUS_WEEK_OFFSET = -7;

const meta: Meta<FdUiWeekPickerComponent> = {
    title: 'Components/Week Picker',
    component: FdUiWeekPickerComponent,
    tags: ['autodocs'],
};

export default meta;
type Story = StoryObj<FdUiWeekPickerComponent>;

export const CurrentWeek: Story = {
    args: {
        panelTitle: 'Choose a week',
        currentWeekLabel: 'Current week',
        returnToCurrentWeekLabel: 'Return to current week',
        previousWeekAriaLabel: 'Previous week',
        nextWeekAriaLabel: 'Next week',
        openCalendarAriaLabel: 'Open week calendar',
    },
};

export const HistoricalWeek: Story = {
    args: {
        value: fdUiAddLocalDays(fdUiStartOfLocalWeek(new Date()), PREVIOUS_WEEK_OFFSET),
        panelTitle: 'Choose a week',
        currentWeekLabel: 'Current week',
        returnToCurrentWeekLabel: 'Return to current week',
    },
};
