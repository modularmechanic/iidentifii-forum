/** Every route in one place, so links and navigation cannot drift apart. */
const Paths = {
  Home: '/',
  DiscussionPattern: '/discussions/:id',
  NewDiscussion: '/discussions/new',
  EditDiscussionPattern: '/discussions/:id/edit',
  editDiscussion: (id: string) => `/discussions/${id}/edit`,
  discussion: (id: string) => `/discussions/${id}`,
  Register: '/register',
  CheckInbox: '/register/check-inbox',
  checkInbox: (email: string) => `/register/check-inbox?email=${encodeURIComponent(email)}`,
  VerifyEmail: '/verify-email',
  Login: '/login',
  LoginCode: '/login/code',
  ForgotPassword: '/forgot-password',
  ResetPassword: '/reset-password',
} as const;

export default Paths;
