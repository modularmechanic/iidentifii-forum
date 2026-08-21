/** Every route in one place, so links and navigation cannot drift apart. */
const Paths = {
  Home: '/',
  DiscussionPattern: '/discussions/:id',
  discussion: (id: string) => `/discussions/${id}`,
  Register: '/register',
  CheckInbox: '/register/check-inbox',
  checkInbox: (email: string) => `/register/check-inbox?email=${encodeURIComponent(email)}`,
  VerifyEmail: '/verify-email',
  Login: '/login',
} as const;

export default Paths;
