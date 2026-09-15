# WeeklyGoals consumer contracts

Own stable read models and SendWeeklyGoalRemindersCommand. The reminder request is IRequest<int>, not a transactional ICommand: its handler preserves per-batch saves. Do not restore the unused IWeeklyGoalReadService. Use canonical project/folder namespaces and no aggregates, repositories, handlers or host types.
