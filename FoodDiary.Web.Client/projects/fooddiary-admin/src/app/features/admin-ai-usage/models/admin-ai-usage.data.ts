export type AdminAiUsageDaily = {
  date: string;
  totalTokens: number;
  inputTokens: number;
  outputTokens: number;
};

export type AdminAiUsageBreakdown = {
  key: string;
  totalTokens: number;
  inputTokens: number;
  outputTokens: number;
};

export type AdminAiUsageUser = {
  id: string;
  email: string;
  totalTokens: number;
  inputTokens: number;
  outputTokens: number;
};

export type AdminAiUsageSummary = {
  totalTokens: number;
  inputTokens: number;
  outputTokens: number;
  byDay: AdminAiUsageDaily[];
  byOperation: AdminAiUsageBreakdown[];
  byModel: AdminAiUsageBreakdown[];
  byUser: AdminAiUsageUser[];
};
